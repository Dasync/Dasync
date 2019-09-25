namespace Dasync.Modeling
{
    public interface IPropertyBag
    {
        IProperty FindProperty(string name);

        object this[string name] { get; }
    }
}
