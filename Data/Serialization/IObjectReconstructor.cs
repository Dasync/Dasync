namespace Dasync.Serialization
{
    public interface IObjectReconstructor
    {
        void OnValueStart(ValueInfo info);
        void OnValue(object value);
        void OnValueEnd();
    }
}
