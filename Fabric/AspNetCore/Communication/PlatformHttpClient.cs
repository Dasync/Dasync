using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AspNetCore.Platform;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.Modeling;
using Dasync.Serialization;

namespace Dasync.AspNetCore.Communication
{
    public interface IPlatformHttpClient
    {
        Task<RoutineInfo> ScheduleRoutineAsync(ExecuteRoutineIntent intent, CancellationToken ct);
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
            var uri = _serviceHttpConfigurator.GetUrl(_serviceDefinition, intent);

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
                        throw new InvalidOperationException($"Unexpected HTTP '{statusCode}' response:\r\n{await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (Exception)
                {
                    await Task.Delay(5_000);
                }
            }
        }
    }
}
