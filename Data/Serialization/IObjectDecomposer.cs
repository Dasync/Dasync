using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface IObjectDecomposer
    {
        IValueContainer Decompose(object value);
    }
}
