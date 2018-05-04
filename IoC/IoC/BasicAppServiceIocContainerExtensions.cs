namespace Dasync.Ioc
{
    public static class BasicAppServiceIocContainerExtensions
    {
        public static void DefineService<TService, TImplementation>(
            this BasicAppServiceIocContainer container)
            where TImplementation : TService
        {
            container.DefineService(typeof(TService), typeof(TImplementation));
        }

        public static void DefineExternalService<TService>(
            this BasicAppServiceIocContainer container)
        {
            container.DefineExternalService(typeof(TService));
        }
    }
}
