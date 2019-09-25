using System.Reflection;

namespace Dasync.Modeling
{
    public class ModelRefiner
    {
        public static void Refine(CommunicationModelBuilder builder)
        {
            foreach (var serviceDefinition in builder.Model.Services)
            {
                FindMethods(serviceDefinition);
                FindEvents(serviceDefinition);
            }
        }

        public static void FindMethods(IMutableServiceDefinition serviceDefinition)
        {
            if (serviceDefinition.Implementation != null)
            {
                foreach (var methodInfo in serviceDefinition.Implementation.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (serviceDefinition.FindMethod(methodInfo.Name) == null &&
                        (methodInfo.IsQueryCandidate() || methodInfo.IsCommandCandidate()))
                        serviceDefinition.GetMethod(methodInfo.Name);
                }
            }
            else
            {
                foreach (var interfaceType in serviceDefinition.Interfaces)
                {
                    foreach (var methodInfo in interfaceType.GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        if (serviceDefinition.FindMethod(methodInfo.Name) == null &&
                            (methodInfo.IsQueryCandidate() || methodInfo.IsCommandCandidate()))
                            serviceDefinition.GetMethod(methodInfo.Name);
                    }
                }
            }
        }

        public static void FindEvents(IMutableServiceDefinition serviceDefinition)
        {
            if (serviceDefinition.Implementation != null)
            {
                foreach (var eventInfo in serviceDefinition.Implementation.GetEvents(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (serviceDefinition.FindEvent(eventInfo.Name) == null && eventInfo.IsEventCandidate())
                        serviceDefinition.GetEvent(eventInfo.Name);
                }
            }
            else
            {
                foreach (var interfaceType in serviceDefinition.Interfaces)
                {
                    foreach (var eventInfo in interfaceType.GetEvents(
                        BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (serviceDefinition.FindEvent(eventInfo.Name) == null && eventInfo.IsEventCandidate())
                            serviceDefinition.GetEvent(eventInfo.Name);
                    }
                }
            }
        }
    }
}
