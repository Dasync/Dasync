using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public class MethodDefinitionBuilder
    {
        public MethodDefinitionBuilder(IMutableMethodDefinition methodDefinition)
        {
            MethodDefinition = methodDefinition;
        }

        public IMutableMethodDefinition MethodDefinition { get; private set; }

        public MethodDefinitionBuilder Ignore()
        {
            MethodDefinition.IsRoutine = false;
            return this;
        }
    }
}
