using System;
using Dasync.ValueContainer;

namespace Dasync.Serialization
{
    public interface ISerializedValueContainer : IValueContainer
    {
        string GetContentType();

        object GetSerializedForm();
    }

    public class SerializedValueContainer : ISerializedValueContainer
    {
        private Lazy<IValueContainer> _lazyValueContainer;
        private string _contentType;
        private object _serializedForm;

        public SerializedValueContainer(
            string contentType,
            object serializedForm,
            object state,
            Func<string, object, object, IValueContainer> deserializeFunc)
        {
            _contentType = contentType;
            _serializedForm = serializedForm;
            _lazyValueContainer = new Lazy<IValueContainer>(() => deserializeFunc(contentType, serializedForm, state));
        }

        public string GetContentType() => _contentType;

        public object GetSerializedForm() => _serializedForm;

        public int GetCount() => _lazyValueContainer.Value.GetCount();

        public string GetName(int index) => _lazyValueContainer.Value.GetName(index);

        public Type GetType(int index) => _lazyValueContainer.Value.GetType(index);

        public object GetValue(int index) => _lazyValueContainer.Value.GetValue(index);

        public void SetValue(int index, object value) => _lazyValueContainer.Value.SetValue(index, value);
    }
}
