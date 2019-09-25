namespace Dasync.Modeling
{
    public interface ICommunicationModelEnricher
    {
        void Enrich(IMutableCommunicationModel model);
    }
}
