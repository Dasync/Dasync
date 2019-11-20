using System;
using System.Threading.Tasks;
using Dasync.EETypes.Intents;
using Dasync.ValueContainer;

namespace Dasync.ExecutionEngine.IntrinsicFlow
{
    public struct WhenAllInputParameters : IValueContainer
    {
        public Task[] tasks;
        public ExecuteRoutineIntent[] intents;
        public Type itemType;

        public int GetCount() => 3;

        public string GetName(int index)
        {
            switch (index)
            {
                case 0: return nameof(tasks);
                case 1: return nameof(intents);
                case 2: return nameof(itemType);
                default: throw new IndexOutOfRangeException();
            }
        }

        public Type GetType(int index)
        {
            switch (index)
            {
                case 0: return typeof(Task[]);
                case 1: return typeof(ExecuteRoutineIntent[]);
                case 2: return typeof(Type);
                default: throw new IndexOutOfRangeException();
            }
        }

        public object GetValue(int index)
        {
            switch (index)
            {
                case 0: return tasks;
                case 1: return intents;
                case 2: return itemType;
                default: throw new IndexOutOfRangeException();
            }
        }

        public void SetValue(int index, object value)
        {
            switch (index)
            {
                case 0: tasks = (Task[])value; break;
                case 1: intents = (ExecuteRoutineIntent[])value; break;
                case 2: itemType = (Type)value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}
