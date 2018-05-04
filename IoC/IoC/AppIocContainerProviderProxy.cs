namespace Dasync.Ioc
{
    public sealed class AppIocContainerProviderProxy : IAppIocContainerProvider
    {
        public sealed class Holder
        {
            public IAppIocContainerProvider Provider { get; set; }
        }

        private readonly Holder _holder;

        public AppIocContainerProviderProxy(Holder holder)
        {
            _holder = holder;
        }

        public IAppServiceIocContainer GetAppIocContainer() => _holder?.Provider?.GetAppIocContainer();
    }
}
