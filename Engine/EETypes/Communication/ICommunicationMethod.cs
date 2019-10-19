using Microsoft.Extensions.Configuration;

namespace Dasync.EETypes.Communication
{
    public interface ICommunicationMethod
    {
        string Type { get; }

        ICommunicator Create(IConfiguration configuration);
    }
}
