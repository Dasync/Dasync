using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Dasync.Modeling
{
    public class ModelRefiner
    {
        public static void Refine(CommunicationModelBuilder builder)
        {
            foreach (var serviceDefinition in builder.Model.Services)
            {
                FindMethods(serviceDefinition);
            }
        }

        public static void FindMethods(IMutableServiceDefinition serviceDefinition)
        {
            if (serviceDefinition.Implementation != null)
            {
                foreach (var methodInfo in serviceDefinition.Implementation.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (serviceDefinition.FindMethod(methodInfo.Name) == null && methodInfo.IsRoutineCandidate())
                        serviceDefinition.GetMethod(methodInfo.Name);
                }
            }

            foreach (var interfaceType in serviceDefinition.Interfaces)
            {
                foreach (var methodInfo in interfaceType.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (serviceDefinition.FindMethod(methodInfo.Name) == null && methodInfo.IsRoutineCandidate())
                        serviceDefinition.GetMethod(methodInfo.Name);
                }
            }
        }
    }
}
