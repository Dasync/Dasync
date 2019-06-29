using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public class ServiceDefinitionBuilder
    {
        public ServiceDefinitionBuilder(IMutableServiceDefinition serviceDefinition)
        {
            ServiceDefinition = serviceDefinition;
        }

        public IMutableServiceDefinition ServiceDefinition { get; private set; }
    }
}
