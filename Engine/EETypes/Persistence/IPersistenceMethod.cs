using Microsoft.Extensions.Configuration;

namespace Dasync.EETypes.Persistence
{
    public interface IPersistenceMethod
    {
        string Type { get; }

        IMethodStateStorage CreateMethodStateStorage(IConfiguration configuration);
    }
}
