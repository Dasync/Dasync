using System;

namespace Dasync.EETypes.Fabric
{
    public interface IRoutinePollInterval
    {
        TimeSpan Suggest(int iteration);
    }
}
