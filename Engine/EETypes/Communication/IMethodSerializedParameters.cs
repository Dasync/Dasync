namespace Dasync.EETypes.Communication
{
    public interface IMethodSerializedParameters
    {
        string ContentType { get; }

        object SerializedForm { get; }
    }
}
