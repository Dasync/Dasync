using Dasync.Modeling;

namespace Dasync.ExecutionEngine.IntrinsicFlow
{
    internal class IntrinsicCommunicationModel
    {
        public static ICommunicationModel Instance =
            CommunicationModelBuilder.Build(_ => _
            .Service<IntrinsicRoutines>(service =>
            {
                service.Command("WhenAll", cmd =>
                {
                    cmd.MethodDefinition.AddProperty("aggregate", true);
                });
            }));

        public static IServiceDefinition IntrinsicRoutinesServiceDefinition =
            Instance.FindServiceByImplementation(typeof(IntrinsicRoutines));
    }
}
