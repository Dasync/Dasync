using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public class ValueContainerReader : IValueReader
    {
        private readonly IValueContainer _container;

        public ValueContainerReader(IValueContainer container)
        {
            _container = container;
        }
        public void Read(IObjectReconstructor reconstructor, ISerializer serializer)
        {
            var count = _container.GetCount();
            for (var i = 0; i < count; i++)
            {
                var valueInfo = new ValueInfo
                {
                    Name = _container.GetName(i),
                    Type = _container.GetType(i).ToTypeSerializationInfo(),
                };
                reconstructor.OnValueStart(valueInfo);
                reconstructor.OnValue(_container.GetValue(i));
                reconstructor.OnValueEnd();
            }
        }

        public void Dispose()
        {
        }
    }
}
