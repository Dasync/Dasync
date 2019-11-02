namespace Dasync.Modeling
{
    public interface ICommunicationModelEnricher
    {
        void Enrich(IMutableCommunicationModel model, bool rootOnly = false);

        void Enrich(IMutableServiceDefinition service, bool serviceOnly = false);

        void Enrich(IMutableMethodDefinition method);
    }
}
