namespace Dasync.EETypes.Communication
{
    public interface IMethodSerializedParameters
    {
        string Format { get; }

        object SerializedForm { get; }
    }
}
