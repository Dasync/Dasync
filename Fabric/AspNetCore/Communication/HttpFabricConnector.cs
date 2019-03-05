using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.Fabric.Sample.Base;
using Dasync.Modeling;
using Dasync.Serialization;

namespace Dasync.AspNetCore.Communication
{
    public class HttpFabricConnector : IFabricConnector
    {
        private readonly IServiceDefinition _serviceDefinition;
        private readonly IServiceHttpConfigurator _serviceHttpConfigurator;
        private readonly ISerializerFactorySelector _serializerFactorySelector;
        private readonly ISerializer _dasyncJsonSerializer;
        private readonly HttpClient _httpClient;

        public HttpFabricConnector(
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

        public async Task<ActiveRoutineInfo> ScheduleRoutineAsync(ExecuteRoutineIntent intent, CancellationToken ct)
        {
            var json = _dasyncJsonSerializer.SerializeToString(intent);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/dasync+json");

            var uri = _serviceHttpConfigurator.GetUrl(_serviceDefinition, intent);
            var response = await _httpClient.PutAsync(uri, content, ct);

            var statusCode = (int)response.StatusCode;
            if (statusCode == DasyncHttpCodes.Succeeded || statusCode == DasyncHttpCodes.Faulted || statusCode == DasyncHttpCodes.Canceled)
            {
                TaskResult taskResult;
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    taskResult = _dasyncJsonSerializer.Deserialize<TaskResult>(stream);
                }

                return new ActiveRoutineInfo
                {
                    Result = taskResult
                };
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public Task<ActiveRoutineInfo> ScheduleContinuationAsync(ContinueRoutineIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        #region Events

        public Task SubscribeForEventAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector publisherFabricConnector)
        {
            throw new NotImplementedException();
        }

        public Task OnEventSubscriberAddedAsync(EventDescriptor eventDesc, EventSubscriberDescriptor subscriber, IFabricConnector subsriberFabricConnector)
        {
            return Task.CompletedTask;
        }

        public Task<ActiveRoutineInfo> PollRoutineResultAsync(ActiveRoutineInfo info, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task PublishEventAsync(RaiseEventIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Triggers

        public Task RegisterTriggerAsync(RegisterTriggerIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeToTriggerAsync(SubscribeToTriggerIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task ActivateTriggerAsync(ActivateTriggerIntent intent, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
