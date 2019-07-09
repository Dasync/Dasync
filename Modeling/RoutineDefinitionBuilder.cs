using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Modeling
{
    public class RoutineDefinitionBuilder
    {
        public RoutineDefinitionBuilder(IMutableRoutineDefinition routineDefinition)
        {
            Routine = routineDefinition;
        }

        public IMutableRoutineDefinition Routine { get; private set; }
    }
}
