using System;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface ISerializedValueContainer : IValueContainer
    {
        string GetFormat();

        object GetSerializedForm();
    }

    public class SerializedValueContainer : ISerializedValueContainer
    {
        private Lazy<IValueContainer> _lazyValueContainer;
        private string _format;
        private object _serializedForm;

        public SerializedValueContainer(
            string format,
            object serializedForm,
            object state,
            Func<string, object, object, IValueContainer> deserializeFunc)
        {
            _format = format;
            _serializedForm = serializedForm;
            _lazyValueContainer = new Lazy<IValueContainer>(() => deserializeFunc(_format, _serializedForm, state));
        }

        public SerializedValueContainer(
            object serializedForm,
            ISerializer serializer)
        {
            _format = serializer.Format;
            _serializedForm = serializedForm;
            _lazyValueContainer = new Lazy<IValueContainer>(() =>
            {
                if (_serializedForm is string text)
                    return serializer.Deserialize<ValueContainer.ValueContainer>(text);
                else
                    return serializer.Deserialize<ValueContainer.ValueContainer>((byte[])_serializedForm);
            });
        }

        public SerializedValueContainer(
            string format,
            object serializedForm,
            ISerializerProvider serializerProvider)
        {
            _format = format;
            _serializedForm = serializedForm;
            _lazyValueContainer = new Lazy<IValueContainer>(() =>
            {
                var serializer = serializerProvider.GetSerializer(_format);
                if (_serializedForm is string text)
                    return serializer.Deserialize<ValueContainer.ValueContainer>(text);
                else
                    return serializer.Deserialize<ValueContainer.ValueContainer>((byte[])_serializedForm);
            });
        }

        public string GetFormat() => _format;

        public object GetSerializedForm() => _serializedForm;

        public int GetCount() => _lazyValueContainer.Value.GetCount();

        public string GetName(int index) => _lazyValueContainer.Value.GetName(index);

        public Type GetType(int index) => _lazyValueContainer.Value.GetType(index);

        public object GetValue(int index) => _lazyValueContainer.Value.GetValue(index);

        public void SetValue(int index, object value) => _lazyValueContainer.Value.SetValue(index, value);
    }
}
