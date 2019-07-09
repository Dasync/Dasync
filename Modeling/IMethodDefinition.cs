using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Dasync.Modeling
{
    public interface IMethodDefinition : IPropertyBag
    {
        IServiceDefinition Service { get; }

        MethodInfo MethodInfo { get; }
    }
}
