namespace Dasync.ValueContainer
{
    public interface IValueContainerFactory
    {
        IValueContainer Create();
    }

    public interface IValueContainerFactory<T> where T : IValueContainer
    {
        T Create();
    }
}
