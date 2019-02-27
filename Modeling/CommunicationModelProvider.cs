namespace Dasync.Modeling
{
    public interface ICommunicationModelProvider
    {
        ICommunicationModel Model { get; }
    }

    public class CommunicationModelProvider : ICommunicationModelProvider
    {
        public class Holder
        {
            public ICommunicationModel Model { get; set; } = new CommunicationModel();
        }

        private readonly Holder _holder;

        public CommunicationModelProvider(Holder holder)
        {
            _holder = holder;
        }

        public ICommunicationModel Model => _holder.Model;
    }
}
