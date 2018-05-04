//using System;
//using System.Collections.Generic;

//namespace Dasync.ServiceRegistry
//{
//    public interface IAppServiceDiscoveryFromCodeMarkup
//    {
//        IEnumerable<Type> DiscoverServiceTypes();
//    }

//    public class AppServiceDiscoveryFromCodeMarkup : IAppServiceDiscoveryFromCodeMarkup
//    {
//        public IEnumerable<Type> DiscoverServiceTypes()
//        {
//            // TODO: 
//            yield break;
//        }
//    }

//    public interface IAppServiceRegistrationInfoExtractor
//    {
//        ServiceRegistrationInfo Extract(Type serviceType);
//    }

//    public class AppServiceRegistrationInfoExtractor : IAppServiceRegistrationInfoExtractor
//    {
//        public ServiceRegistrationInfo Extract(Type serviceType)
//        {
//            throw new NotImplementedException();
//        }
//    }

//    public interface IAppServiceDiscoveryFromRuntimeCollection
//    {
//        ICollection<ServiceRegistrationInfo> Services { get; }
//    }

//    public class AppServiceDiscoveryFromRuntimeCollection : IAppServiceDiscoveryFromRuntimeCollection
//    {
//        public AppServiceDiscoveryFromRuntimeCollection()
//        {
//            Services = new List<ServiceRegistrationInfo>();
//        }

//        public ICollection<ServiceRegistrationInfo> Services { get; }
//    }

//    public static class Extensions_IAppServiceDiscoveryFromRuntimeCollection
//    {
//        public static void Add(this IAppServiceDiscoveryFromRuntimeCollection collection, params Type[] serviceTypes)
//        {
//            foreach (var serviceType in serviceTypes)
//            {
//                var serviceRegistrationInfo = new ServiceRegistrationInfo
//                {
//                    QualifiedServiceTypeName = serviceType.AssemblyQualifiedName,
//                    QualifiedImplementationTypeName =
//                        (serviceType.IsClass && !serviceType.IsAbstract)
//                        ? serviceType.AssemblyQualifiedName
//                        : null,
//                    IsSingleton = true,
//                    IsExternal = false
//                };
//                collection.Services.Add(serviceRegistrationInfo);
//            }
//        }
//    }
//}
