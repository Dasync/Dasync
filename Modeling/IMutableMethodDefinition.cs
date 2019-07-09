using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public interface IMutableMethodDefinition : IMethodDefinition, IMutablePropertyBag
    {
        new IMutableServiceDefinition Service { get; }

        new bool IsRoutine { get; set; }
    }
}
