using Dasync.Modeling;

namespace Dasync.ExecutionEngine.IntrinsicFlow
{
    internal class IntrinsicCommunicationModel
    {
        public static ICommunicationModel Instance =
            CommunicationModelBuilder.Build(_ => _
            .Service<IntrinsicRoutines>(_ => { }));

        public static IServiceDefinition IntrinsicRoutinesServiceDefinition =
            Instance.FindServiceByImplementation(typeof(IntrinsicRoutines));
    }
}
