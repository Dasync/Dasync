namespace Dasync.Modeling
{
    public class Property : IProperty
    {
        public Property(string name)
        {
            Name = name;
        }

        public Property(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public object Value { get; set; }
    }
}
