using System;

namespace Dasync.Fabric.Sample.Base
{
    public interface IRoutinePollInterval
    {
        TimeSpan Suggest(int iteration);
    }
}
