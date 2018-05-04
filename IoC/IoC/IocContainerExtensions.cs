namespace Dasync.Ioc
{
    public static class IocContainerExtensions
    {
        public static T Resolve<T>(this IIocContainer container) => (T)container.Resolve(typeof(T));
    }
}
