using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public interface IMutableRoutineDefinition : IRoutineDefinition, IMutablePropertyBag
    {
        new IMutableServiceDefinition Service { get; }
    }
}
