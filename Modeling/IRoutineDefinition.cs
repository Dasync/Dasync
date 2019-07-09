using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public interface IRoutineDefinition : IPropertyBag
    {
        IServiceDefinition Service { get; }
    }
}
