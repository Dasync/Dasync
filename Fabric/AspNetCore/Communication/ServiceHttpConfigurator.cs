using System;
using System.Net.Http;
using Dasync.EETypes.Intents;
using Dasync.Modeling;
using DasyncAspNetCore;
using Microsoft.Extensions.Options;

namespace Dasync.AspNetCore.Communication
{
    public interface IServiceHttpConfigurator
    {
        void ConfigureBase(HttpClient httpClient, IServiceDefinition serviceDefinition);

        Uri GetUrl(IServiceDefinition serviceDefinition);
    }

    public class DefaultServiceHttpConfigurator : IServiceHttpConfigurator
    {
        private readonly string _apiPath;

        public DefaultServiceHttpConfigurator(IOptionsMonitor<DasyncOptions> optionsMonitor)
        {
            var options = optionsMonitor.CurrentValue;
            _apiPath = options.ApiPath;
            if (_apiPath == null)
                _apiPath = DasyncOptions.Defaults.ApiPath;
        }

        public virtual void ConfigureBase(HttpClient httpClient, IServiceDefinition serviceDefinition)
        {
            httpClient.BaseAddress = new Uri($"http://{serviceDefinition.Name}");
            httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public virtual Uri GetUrl(IServiceDefinition serviceDefinition)
        {
            return new Uri($"{_apiPath}/{serviceDefinition.Name}", UriKind.Relative);
        }
    }
}
