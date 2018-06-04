using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AzureStorage;
using Dasync.CloudEvents;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Fabric;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Proxy;
using Dasync.EETypes.Transitions;
using Dasync.ExecutionEngine;
using Dasync.FabricConnector.AzureStorage;
using Dasync.Proxy;
using Dasync.Serialization;
using Dasync.ServiceRegistry;
using Dasync.ValueContainer;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using FunctionExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace Dasync.Fabric.AzureFunctions
{
    public interface IAzureFunctionsFabric
    {
        Task ProcessMessageAsync(
#warning need to take in function configuration - different connection
            CloudQueueMessage message,
            FunctionExecutionContext context,
            DateTimeOffset messageReceiveTime,
            ILogger logger,
            CancellationToken ct);

        Task<HttpResponseMessage> ProcessRequestAsync(
#warning need to take in function configuration - is gateway?
            HttpRequestMessage request,
            FunctionExecutionContext context,
            DateTimeOffset requestStartTime,
            ILogger logger,
            CancellationToken ct);
    }

    public class AzureFunctionsFabricSettings
    {
        public string FunctionsDirectory { get; set; }
    }

    public class AzureFunctionsFabric : IFabric, IAzureFunctionsFabric
    {
        private struct ExecutingFuncionInfo
        {
            public string FunctionName { get; set; }

            public ICloudQueue Queue { get; set; }
        }

        private static readonly System.Threading.AsyncLocal<ExecutingFuncionInfo> _currentFunction
            = new System.Threading.AsyncLocal<ExecutingFuncionInfo>();

        private readonly ITransitionRunner _transitionRunner;
        private readonly INumericIdGenerator _idGenerator;
        private readonly ISerializer _defaultSerializer;
        private readonly ICloudStorageAccount _storageAccount;
        private readonly Dictionary<string, ICloudQueue> _functionToQueueMap
            = new Dictionary<string, ICloudQueue>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ICloudQueue> _serviceToQueueMap
            = new Dictionary<string, ICloudQueue>();
        private readonly AzureFunctionsFabricSettings _settings;
        private readonly string _storageAccountConnectionString;
        private readonly ICloudTable _routinesTable;
        private readonly ICloudTable _servicesTable;
        private readonly IServiceProxyBuilder _serviceProxyBuilder;
        private readonly IRoutineMethodResolver _routineMethodResolver;
        private readonly IMethodInvokerFactory _methodInvokerFactory;
        private readonly IServiceRegistry _serviceRegistry;

        public AzureFunctionsFabric(
            ISerializerFactorySelector serializerFactorySelector,
            INumericIdGenerator idGenerator,
            ITransitionRunner transitionRunner,
            IAzureWebJobsEnviromentalSettings azureWebJobsEnviromentalSettings,
            ICloudStorageAccountFactory cloudStorageAccountFactory,
            AzureFunctionsFabricSettings settings,
            IServiceProxyBuilder serviceProxyBuilder,
            IRoutineMethodResolver routineMethodResolver,
            IMethodInvokerFactory methodInvokerFactory,
            IServiceRegistry serviceRegistry)
        {
            _transitionRunner = transitionRunner;
            _idGenerator = idGenerator;
            _settings = settings;
            _serviceProxyBuilder = serviceProxyBuilder;
            _routineMethodResolver = routineMethodResolver;
            _methodInvokerFactory = methodInvokerFactory;
            _serviceRegistry = serviceRegistry;

#warning Need configurable serializer
            // Hard-code this for now.
            _defaultSerializer = serializerFactorySelector.Select("dasyncjson").Create();

            _storageAccountConnectionString = azureWebJobsEnviromentalSettings.DefaultStorageConnectionString;
            _storageAccount = cloudStorageAccountFactory.Create(_storageAccountConnectionString);

            //#warning make sure that site name is alpha-numeric and does not start with a number
            //            var prefix = azureWebJobsEnviromentalSettings.WebSiteName.ToLowerInvariant();
            var prefix = "";
            _routinesTable = _storageAccount.TableClient.GetTableReference(prefix + "routines");
            _servicesTable = _storageAccount.TableClient.GetTableReference(prefix + "services");
        }

        public IFabricConnector GetConnector(ServiceId serviceId)
        {
            if (!_serviceToQueueMap.TryGetValue(serviceId.ServiceName, out var transitionsQueue))
                // Fall back to the queue of a function currently being executed.
                // This is needed for the Intrinsic Routines.
                // Such code is not needed for the gateway, and you are not allowed to call
                // Intrinsic Routines directly from the gateway.
                transitionsQueue = _currentFunction.Value.Queue;

            if (transitionsQueue == null)
                throw new InvalidOperationException(
                    $"Cannot find queue for the service '{serviceId.ServiceName}'.");

            var configuration = new AzureStorageFabricConnectorConfiguration
            {
                SerializerFormat = "dasyncjson",
                TransitionsQueueName = transitionsQueue.Name,
                RoutinesTableName = _routinesTable.Name,
                ServicesTableName = _servicesTable.Name,
                StorageAccountName = ConnectionStringParser.GetAccountName(_storageAccountConnectionString)
            };

            return new AzureStorageFabricConnectorWithConfiguration(
                serviceId,
                _idGenerator,
                transitionsQueue,
                _routinesTable,
                _defaultSerializer,
                configuration);
        }

        public Task InitializeAsync(CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(_settings.FunctionsDirectory) &&
                Directory.Exists(_settings.FunctionsDirectory))
            {
                var subDirectories = Directory.GetDirectories(
                    _settings.FunctionsDirectory, "*", SearchOption.TopDirectoryOnly);

                foreach (var subDirectory in subDirectories)
                {
                    if (!GlobalStartup.TryGetStartupSettings(subDirectory, out var startupSettings))
                        continue;

                    if (startupSettings.ServiceNames == null || startupSettings.ServiceNames.Count == 0)
                        continue;

                    if (!GlobalStartup.TryGetFunctionSettings(subDirectory, out var functionSettings))
                        continue;

                    var queueBinding = functionSettings.Bindings?.SingleOrDefault(b =>
                        (string.IsNullOrEmpty(b.Direction) || b.Direction == "in")
                        && b.Type == "queueTrigger" && !string.IsNullOrEmpty(b.QueueName));

                    if (queueBinding == null)
                        continue;

                    var queue = _storageAccount.QueueClient.GetQueueReference(queueBinding.QueueName);

                    var functionName = Path.GetFileName(subDirectory);
                    _functionToQueueMap[functionName] = queue;

                    foreach (var serviceName in startupSettings.ServiceNames)
                        _serviceToQueueMap[serviceName] = queue;
                }
            }

            return Task.FromResult(true);
        }

        public Task StartAsync(CancellationToken ct)
        {
            return Task.FromResult(true);
        }

        public Task TerminateAsync(CancellationToken ct)
        {
            return Task.FromResult(true);
        }

        public async Task ProcessMessageAsync(
            CloudQueueMessage message,
            FunctionExecutionContext context,
            DateTimeOffset messageReceiveTime,
            ILogger logger,
            CancellationToken ct)
        {
            _functionToQueueMap.TryGetValue(context.FunctionName, out var queue);
            _currentFunction.Value = new ExecutingFuncionInfo
            {
                FunctionName = context.FunctionName,
                Queue = queue
            };

            try
            {

#warning Keep message invisible while transitioning

                var eventEnvelope = JsonConvert.DeserializeObject<RoutineEventEnvelope>(
                    message.AsString, CloudEventsSerialization.JsonSerializerSettings);
                // Free memory
                message.SetMessageContent((string)null);
                message.SetMessageContent((byte[])null);

                var transitionCarrier = new AzureStorageTransitionCarrier(
                    eventEnvelope, _routinesTable, _servicesTable, _defaultSerializer);
                var transitionData = transitionCarrier;

                var concurrentExecutionDetected = false;
                try
                {
                    await _transitionRunner.RunAsync(transitionCarrier, transitionData, ct);
                }
                catch (ConcurrentTransitionException)
                {
                    concurrentExecutionDetected = true;
                }

                if (concurrentExecutionDetected)
                {
                    var transitionInfo = await transitionCarrier.GetTransitionDescriptorAsync(ct);
                    if (transitionInfo.Type != TransitionType.InvokeRoutine)
                    {
#warning Host has a setting of max re-tries. Re-enqueue another message? Delay also?
                        throw new Exception("re-try");

                        //// Re-try message by resetting its invisibility time.
                        //await transitionsQueue.UpdateMessageAsync(
                        //    message, TimeSpan.Zero, MessageUpdateFields.Visibility, ct);
                        //continue;
                    }
                    // If there are 2 or more messages that try to invoke the routine
                    // for the first time, accept the first message and drop the rest.
                }

                //await transitionsQueue.DeleteMessageAsync(message.Id, message.PopReceipt, ct);
            }
            finally
            {
                _currentFunction.Value = default;
            }
        }

        public async Task<HttpResponseMessage> ProcessRequestAsync(
            HttpRequestMessage request,
            FunctionExecutionContext context,
            DateTimeOffset requestStartTime,
            ILogger logger,
            CancellationToken ct)
        {
            _currentFunction.Value = new ExecutingFuncionInfo
            {
                FunctionName = context.FunctionName
            };

            try
            {
#warning Create an HTTP connector
                if (request.Method == HttpMethod.Post)
                {
                    string serviceName = null;
                    string routineName = null;
                    long? intentId = null;
                    bool @volatile = false;
                    TimeSpan pollTime = TimeSpan.Zero;

                    foreach (var parameter in request.GetQueryNameValuePairs())
                    {
                        switch (parameter.Key)
                        {
                            case "service":
                                serviceName = parameter.Value;
                                break;

                            case "routine":
                                routineName = parameter.Value;
                                break;

                            case "intent":
                                if (long.TryParse(parameter.Value, out var parsedIntentId))
                                    intentId = parsedIntentId;
                                break;
                        }
                    }

                    foreach (var header in request.Headers)
                    {
                        var firstValue = header.Value.FirstOrDefault();

                        switch (header.Key)
                        {
                            case "Volatile":
                                if (string.IsNullOrEmpty(firstValue) ||
                                    !bool.TryParse(firstValue, out @volatile))
                                    @volatile = true;
                                break;

                            case "Poll-Time":
                                if (!string.IsNullOrEmpty(firstValue))
                                {
                                    if (double.TryParse(firstValue, out var pollTimeInSeconds))
                                    {
                                        pollTime = TimeSpan.FromSeconds(pollTimeInSeconds);
                                    }
                                    else if (TimeSpan.TryParse(firstValue, out var pollTimeTimeSpan))
                                    {
                                        pollTime = pollTimeTimeSpan;
                                    }
                                }
                                break;
                        }
                    }

                    var serviceId = new ServiceId
                    {
                        ServiceName = serviceName
                    };

                    var routineMethodId = new RoutineMethodId
                    {
                        MethodName = routineName
                    };

                    var registration = _serviceRegistry.AllRegistrations
                        .SingleOrDefault(r => r.ServiceName == serviceId.ServiceName);

                    if (registration == null || registration.IsExternal)
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent(
                                registration == null
                                ? "Service not found"
                                : "Cannot invoke external service")
                        };

                    IValueContainer parameterContainer = null;
                    if (request.Content != null)
                    {
                        var routineMethod = _routineMethodResolver.Resolve(registration.ServiceType, routineMethodId);
                        var methodInvoker = _methodInvokerFactory.Create(routineMethod);
                        parameterContainer = methodInvoker.CreateParametersContainer();

#warning change to use a serializer based on the format
                        var paramsJson = await request.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(paramsJson))
                            JsonConvert.PopulateObject(paramsJson, parameterContainer);
                    }

                    var intent = new ExecuteRoutineIntent
                    {
#warning Generate intent ID from function request ID? any benefit (like on re-try)?
                        Id = intentId ?? _idGenerator.NewId(),
                        ServiceId = serviceId,
                        MethodId = routineMethodId,
                        Parameters = parameterContainer,
                        Continuation = null
                    };

                    var connector = GetConnector(serviceId);
                    var routineInfo = await connector.ScheduleRoutineAsync(intent, ct);

                    if (pollTime > TimeSpan.Zero)
                        routineInfo = await PollRoutineAsync(routineInfo, connector, requestStartTime, pollTime, ct);

                    if (routineInfo.Result != null)
                        return CreateResponseFromRoutineResult(routineInfo.Result);

                    var response = new HttpResponseMessage(HttpStatusCode.Accepted);

                    var location = $"{request.RequestUri.AbsolutePath}?service={serviceId.ServiceName}&routineId={routineInfo.RoutineId}";
                    response.Headers.Add("Location", location);

#warning ETag must be in certain format
                    //if (!string.IsNullOrEmpty(routineInfo.ETag))
                    //    response.Headers.ETag = new EntityTagHeaderValue(routineInfo.ETag);

                    return response;
                }
                else if (request.Method == HttpMethod.Get)
                {
                    string serviceName = null;
                    string routineId = null;
                    string routineETag = null;
                    TimeSpan pollTime = TimeSpan.Zero;

                    foreach (var parameter in request.GetQueryNameValuePairs())
                    {
                        switch (parameter.Key)
                        {
                            case "service":
                                serviceName = parameter.Value;
                                break;

                            case "routineId":
                                routineId = parameter.Value;
                                break;

                            case "routineTag":
                                routineETag = parameter.Value;
                                break;
                        }
                    }

                    foreach (var header in request.Headers)
                    {
                        var firstValue = header.Value.FirstOrDefault();

                        switch (header.Key)
                        {
                            case "Poll-Time":
                                if (!string.IsNullOrEmpty(firstValue))
                                {
                                    if (double.TryParse(firstValue, out var pollTimeInSeconds))
                                    {
                                        pollTime = TimeSpan.FromSeconds(pollTimeInSeconds);
                                    }
                                    else if (TimeSpan.TryParse(firstValue, out var pollTimeTimeSpan))
                                    {
                                        pollTime = pollTimeTimeSpan;
                                    }
                                }
                                break;
                        }
                    }

                    var serviceId = new ServiceId
                    {
                        ServiceName = serviceName
                    };

                    var routineInfo = new ActiveRoutineInfo
                    {
                        RoutineId = routineId,
                        ETag = routineETag
                    };

                    var connector = GetConnector(serviceId);

                    routineInfo = await PollRoutineAsync(routineInfo, connector, requestStartTime, pollTime, ct);

                    if (routineInfo.Result != null)
                        return CreateResponseFromRoutineResult(routineInfo.Result);

                    var response = new HttpResponseMessage(HttpStatusCode.NoContent);

#warning ETag must be in certain format
                    //if (!string.IsNullOrEmpty(routineInfo.ETag))
                    //    response.Headers.ETag = new EntityTagHeaderValue(routineInfo.ETag);

                    return response;
                }
            }
            catch (Exception ex)
            {
                return CreateInfrastructureErrorResponse(ex);
            }
            finally
            {
                _currentFunction.Value = default;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private HttpResponseMessage CreateResponseFromRoutineResult(TaskResult result)
        {
            if (result.IsCanceled)
            {
                return new HttpResponseMessage(HttpStatusCode.ResetContent);
            }
            else if (result.IsFaulted)
            {
                var resultJson = JsonConvert.SerializeObject(
                    new
                    {
                        Type = result.Exception.GetType().FullName,
                        result.Exception.Message,
                        result.Exception.StackTrace
                    });
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(resultJson, Encoding.UTF8, "application/json")
                };
            }
            else
            {
                var resultJson = JsonConvert.SerializeObject(result.Value);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(resultJson, Encoding.UTF8, "application/json")
                };
            }
        }

        private HttpResponseMessage CreateInfrastructureErrorResponse(Exception ex)
        {
            var resultJson = JsonConvert.SerializeObject(
                new
                {
                    Type = ex.GetType().FullName,
                    ex.Message,
                    ex.StackTrace
                });
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(resultJson, Encoding.UTF8, "application/json")
            };
        }

        private async Task<ActiveRoutineInfo> PollRoutineAsync(
            ActiveRoutineInfo routineInfo,
            IFabricConnector connector,
            DateTimeOffset requestStartTime,
            TimeSpan maxPollTime,
            CancellationToken ct)
        {
            var stopPollingAt = requestStartTime + maxPollTime;

            for (var i = 0; ; i++)
            {
                routineInfo = await connector.PollRoutineResultAsync(routineInfo, ct);
                if (routineInfo.Result != null)
                    break;

                TimeSpan delayInterval;

                if (routineInfo is IRoutinePollInterval routinePollInterval)
                {
                    delayInterval = routinePollInterval.Suggest(i);
                }
                else
                {
                    delayInterval = TimeSpan.FromSeconds(0.5);
                }

                var resumeAt = DateTimeOffset.Now + delayInterval;
                if (resumeAt > stopPollingAt)
                    delayInterval = stopPollingAt - DateTimeOffset.Now;

                if (delayInterval <= TimeSpan.Zero)
                    break;

                await Task.Delay(delayInterval);
            }

            return routineInfo;
        }
    }
}
