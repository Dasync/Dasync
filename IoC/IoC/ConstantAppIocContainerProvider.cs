namespace Dasync.Ioc
{
    public sealed class ConstantAppIocContainerProvider : IAppIocContainerProvider
    {
        public ConstantAppIocContainerProvider(IIocContainer container)
        {
            Container = container as IAppServiceIocContainer;
        }

        public IAppServiceIocContainer Container { get; }

        public IAppServiceIocContainer GetAppIocContainer() => Container;
    }
}
