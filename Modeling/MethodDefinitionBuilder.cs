using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public class MethodDefinitionBuilder
    {
        public MethodDefinitionBuilder(IMutableMethodDefinition methodDefinition)
        {
            Method = methodDefinition;
        }

        public IMutableMethodDefinition Method { get; private set; }
    }
}
