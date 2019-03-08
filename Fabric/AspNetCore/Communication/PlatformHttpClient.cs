using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AspNetCore.Platform;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.Modeling;
using Dasync.Serialization;

namespace Dasync.AspNetCore.Communication
{
    public interface IPlatformHttpClient
    {
        Task<RoutineInfo> ScheduleRoutineAsync(ExecuteRoutineIntent intent, CancellationToken ct);

        Task SubscribeToEvent(EventDescriptor eventDesc, ServiceId subscriber, IServiceDefinition publisherServiceDefinition);

        Task PublishEvent(RaiseEventIntent intent, IServiceDefinition subscriberServiceDefinition);
    }

    public class PlatformHttpClient : IPlatformHttpClient
    {
        private readonly IServiceDefinition _serviceDefinition;
        private readonly IServiceHttpConfigurator _serviceHttpConfigurator;
        private readonly ISerializerFactorySelector _serializerFactorySelector;
        private readonly ISerializer _dasyncJsonSerializer;
        private readonly HttpClient _httpClient;

        public PlatformHttpClient(
            IServiceDefinition serviceDefinition,
            ISerializerFactorySelector serializerFactorySelector,
            IServiceHttpConfigurator serviceHttpConfigurator)
        {
            _serviceDefinition = serviceDefinition;
            _serializerFactorySelector = serializerFactorySelector;
            _serviceHttpConfigurator = serviceHttpConfigurator;

            _httpClient = new HttpClient();
            serviceHttpConfigurator.ConfigureBase(_httpClient, _serviceDefinition);

            _dasyncJsonSerializer = serializerFactorySelector.Select("dasync+json").Create();
        }

        public async Task<RoutineInfo> ScheduleRoutineAsync(ExecuteRoutineIntent intent, CancellationToken ct)
        {
            var uri = string.Concat(_serviceHttpConfigurator.GetUrl(_serviceDefinition), "/", intent.MethodId.MethodName);

            var json = _dasyncJsonSerializer.SerializeToString(intent);
            while (true)
            {
                try
                {
                    var content = new StringContent(json);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/dasync+json");

                    var response = await _httpClient.PutAsync(uri, content, ct);

                    var statusCode = (int)response.StatusCode;
                    if (statusCode == DasyncHttpCodes.Succeeded || statusCode == DasyncHttpCodes.Faulted || statusCode == DasyncHttpCodes.Canceled)
                    {
                        TaskResult taskResult;
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            taskResult = _dasyncJsonSerializer.Deserialize<TaskResult>(stream);
                        }

                        return new RoutineInfo
                        {
                            Result = taskResult
                        };
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected HTTP {statusCode} response:\r\n{await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (Exception)
                {
                    await Task.Delay(5_000);
                }
            }
        }

        public async Task SubscribeToEvent(EventDescriptor eventDesc, ServiceId subscriber, IServiceDefinition publisherServiceDefinition)
        {
            var uri = string.Concat(_serviceHttpConfigurator.GetUrl(publisherServiceDefinition), "/", eventDesc.EventId.EventName, "?subscribe&service=", subscriber.ServiceName);

            if (!string.IsNullOrEmpty(subscriber.ProxyName))
                uri += string.Concat("&proxy=", subscriber.ProxyName);

            var response = await _httpClient.PutAsync(uri, null);

            var statusCode = (int)response.StatusCode;
            if (statusCode != DasyncHttpCodes.Succeeded)
                throw new InvalidOperationException($"Unexpected HTTP {statusCode} response:\r\n{await response.Content.ReadAsStringAsync()}");
        }

        public async Task PublishEvent(RaiseEventIntent intent, IServiceDefinition subscriberServiceDefinition)
        {
            var uri = string.Concat(_serviceHttpConfigurator.GetUrl(subscriberServiceDefinition), "?react&event=", intent.EventId.EventName, "&service=", intent.ServiceId.ServiceName);

            var json = _dasyncJsonSerializer.SerializeToString(intent);

            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/dasync+json");

            var response = await _httpClient.PutAsync(uri, content);

            var statusCode = (int)response.StatusCode;
            if (statusCode != DasyncHttpCodes.Succeeded && statusCode != DasyncHttpCodes.Scheduled)
                throw new InvalidOperationException($"Unexpected HTTP {statusCode} response:\r\n{await response.Content.ReadAsStringAsync()}");
        }
    }
}
