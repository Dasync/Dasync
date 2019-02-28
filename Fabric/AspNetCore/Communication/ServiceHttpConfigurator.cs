using System;
using System.Net.Http;
using Dasync.EETypes.Intents;
using Dasync.Modeling;

namespace Dasync.AspNetCore.Communication
{
    public interface IServiceHttpConfigurator
    {
        void ConfigureBase(HttpClient httpClient, IServiceDefinition serviceDefinition);

        Uri GetUrl(IServiceDefinition serviceDefinition, ExecuteRoutineIntent intent);
    }

    internal class DefaultServiceHttpConfigurator : IServiceHttpConfigurator
    {
        public virtual void ConfigureBase(HttpClient httpClient, IServiceDefinition serviceDefinition)
        {
            httpClient.BaseAddress = new Uri($"http://{serviceDefinition.Name}.api");
            //httpClient.BaseAddress = new Uri($"http://localhost:54258");
            httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public virtual Uri GetUrl(IServiceDefinition serviceDefinition, ExecuteRoutineIntent intent)
        {
            var serviceName = intent.ServiceId.ProxyName ?? intent.ServiceId.ServiceName;
            var methodName = intent.MethodId.MethodName;

            return new Uri($"/api/{serviceName}/{methodName}", UriKind.Relative);
        }
    }
}
