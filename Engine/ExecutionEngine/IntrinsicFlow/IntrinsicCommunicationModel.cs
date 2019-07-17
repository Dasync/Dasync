using Dasync.Modeling;

namespace Dasync.ExecutionEngine.IntrinsicFlow
{
    internal class IntrinsicCommunicationModel
    {
        public static ICommunicationModel Instance =
            new CommunicationModelBuilder()
            .Service<IntrinsicRoutines>(_ => { })
            .Model;

        public static IServiceDefinition IntrinsicRoutinesServiceDefinition =
            Instance.FindServiceByImplementation(typeof(IntrinsicRoutines));
    }
}
