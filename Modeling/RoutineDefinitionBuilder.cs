using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public class RoutineDefinitionBuilder
    {
        public RoutineDefinitionBuilder(IMutableRoutineDefinition routineDefinition)
        {
            RoutineDefinition = routineDefinition;
        }

        public IMutableRoutineDefinition RoutineDefinition { get; private set; }

        public RoutineDefinitionBuilder AlternateName(params string[] alternateMethodNames)
        {
            foreach (var altName in alternateMethodNames)
                RoutineDefinition.AddAlternateName(altName);
            return this;
        }
    }
}
