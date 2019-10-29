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
            _lazyValueContainer = new Lazy<IValueContainer>(() => deserializeFunc(format, serializedForm, state));
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
