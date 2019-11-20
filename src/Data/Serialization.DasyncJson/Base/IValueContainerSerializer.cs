using System.Collections.Generic;

namespace Dasync.Serialization
{
    public interface IValueContainerSerializer
    {
        void Serialize(object value, IValueWriter writer,
            IEnumerable<KeyValuePair<string, object>> specialObjects = null);
    }
}
