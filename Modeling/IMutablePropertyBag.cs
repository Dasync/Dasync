namespace Dasync.Modeling
{
    public interface IMutablePropertyBag : IPropertyBag
    {
        new object this[string name] { get; set; }

        void AddProperty(string name, object value);

        void SetProperty(string name, object value);

        bool RemoveProperty(string name);
    }
}
