using Dasync.Modeling;

namespace Dasync.ExecutionEngine.IntrinsicFlow
{
    internal class IntrinsicCommunicationModel
    {
        public static ICommunicationModel Instance =
            CommunicationModelBuilder.Build(_ => _
            .Service<IntrinsicRoutines>(service =>
            {
                service.ServiceDefinition.Type = ServiceType.System;
                service.ServiceDefinition.Name = "_tpl"; // Task Parallel Library
                service.ServiceDefinition.AddAlternateName(nameof(IntrinsicRoutines));

                service.Command("WhenAll", cmd =>
                {
                    cmd.MethodDefinition.AddProperty("aggregate", true);
                });
            }));

        public static IServiceDefinition IntrinsicRoutinesServiceDefinition =
            Instance.FindServiceByImplementation(typeof(IntrinsicRoutines));
    }
}
