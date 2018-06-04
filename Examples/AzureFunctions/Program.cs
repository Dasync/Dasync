using System;
using System.Threading.Tasks;
using Autofac;
using Dasync.Ioc.Autofac;
using Dasync.Ioc.Ninject;
using Ninject;

// To run this demo, follow the steps described in the:
// https://dasyncnet.wordpress.com/2018/05/04/dasync-on-azure-functions/
// You can manually publish this Azure Functions project to an App Service
// and use HTTP request tool to trigger a service method.

namespace DasyncDemo
{
    // Simply returns "Hello".
    public interface IEnglishDictionary
    {
        Task<string> GreetingWord();
    }

    // Simply returns "Hello, {name}!".
    public interface IGreetingService
    {
        Task<string> Greet(string name);
    }

    public class EnglishDictionary : IEnglishDictionary
    {
        public Task<string> GreetingWord() =>
            Task.FromResult("Hello");
    }

    public class GreetingService : IGreetingService
    {
        private IEnglishDictionary _dictionary;

        // Make it properly with dependency injection.
        public GreetingService(IEnglishDictionary dictionary)
            => _dictionary = dictionary;

        public async Task<string> Greet(string name)
        {
            var greetingWord = await _dictionary.GreetingWord();
            return $"{greetingWord}, {name}!";
        }
    }

    public class Startup
    {
        // And some code to configure the IoC container.
        // This example uses Autofac.
        public static IContainer CreateIocContainer()
        {
            var builder = new ContainerBuilder();
            builder
                .RegisterType<EnglishDictionary>()
                .As<IEnglishDictionary>()
                .LocalService();
            builder
                .RegisterType<GreetingService>()
                .As<IGreetingService>()
                .LocalService();
            return builder.Build();
        }

        /* Uncomment this code to use Ninject instead. */
        //public IKernel CreateIocContainer()
        //{
        //    var kernel = new StandardKernel();
        //    kernel
        //        .Bind<IEnglishDictionary>()
        //        .To<EnglishDictionary>()
        //        .AsService();
        //    kernel
        //        .Bind<IGreetingService>()
        //        .To<GreetingService>()
        //        .AsService();
        //    return kernel;
        //}
    }
}
