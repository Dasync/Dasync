using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public class ExternalServiceDefinitionBuilder
    {
        public ExternalServiceDefinitionBuilder(IMutableServiceDefinition serviceDefinition)
        {
            ServiceDefinition = serviceDefinition;
        }

        public IMutableServiceDefinition ServiceDefinition { get; private set; }
    }
}
