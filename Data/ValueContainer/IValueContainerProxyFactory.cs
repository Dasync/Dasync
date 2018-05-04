namespace Dasync.ValueContainer
{
    public interface IValueContainerProxyFactory
    {
        IValueContainer Create(object target);
    }

    public interface IValueContainerProxyFactory<TObject, TContainer>
        where TContainer : IValueContainer
    {
        TContainer Create(TObject target);
    }
}
