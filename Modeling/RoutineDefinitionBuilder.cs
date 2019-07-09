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

        public RoutineDefinitionBuilder AlternativeName(params string[] alternativeMethodNames)
        {
            foreach (var altName in alternativeMethodNames)
                RoutineDefinition.AddAlternativeName(altName);
            return this;
        }
    }
}
