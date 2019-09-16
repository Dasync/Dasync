using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dasync.AspNetCore;
using Dasync.AspNetCore.Communication;
using Dasync.AspNetCore.Errors;
using Dasync.AspNetCore.Json;
using Dasync.AspNetCore.Platform;
using Dasync.EETypes;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Ioc;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Resolvers;
using Dasync.ExecutionEngine.Extensions;
using Dasync.Json;
using Dasync.Modeling;
using Dasync.Proxy;
using Dasync.Serialization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DasyncAspNetCore
{
    public interface IHttpRequestHandler
    {
        Task HandleAsync(PathString basePath, HttpContext context, CancellationToken ct);
    }

    public class HttpRequestHandler : IHttpRequestHandler
    {
        private readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    OverrideSpecifiedNames = true,
                    ProcessDictionaryKeys = true,
                    ProcessExtensionDataNames = true
                }
            }
        };

        private readonly ICommunicationModel _communicationModel;
        private readonly IDomainServiceProvider _domainServiceProvider;
        private readonly IMethodInvokerFactory _methodInvokerFactory;
        private readonly ISerializer _dasyncJsonSerializer;
        private readonly IEventDispatcher _eventDispatcher;
        private readonly IUniqueIdGenerator _idGenerator;
        private readonly ITransitionCommitter _transitionCommitter;
        private readonly IRoutineCompletionNotifier _routineCompletionNotifier;
        private readonly IHttpIntentPreprocessor _intentPreprocessor;
        private readonly IEnumerable<IRoutineTransitionAction> _transitionActions;
        private readonly ITransitionUserContext _transitionUserContext;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;

        private TimeSpan MaxLongPollTime = TimeSpan.FromMinutes(2);

        public HttpRequestHandler(
            ICommunicationModel communicationModel,
            IDomainServiceProvider domainServiceProvider,
            IMethodInvokerFactory methodInvokerFactory,
            ISerializerFactorySelector serializerFactorySelector,
            IEnumerable<IEventDispatcher> eventDispatchers,
            IUniqueIdGenerator idGenerator,
            ITransitionCommitter transitionCommitter,
            IRoutineCompletionNotifier routineCompletionNotifier,
            IEnumerable<IHttpIntentPreprocessor> intentPreprocessors,
            IEnumerable<IRoutineTransitionAction> transitionActions,
            ITransitionUserContext transitionUserContext,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver)
        {
            _communicationModel = communicationModel;
            _domainServiceProvider = domainServiceProvider;
            _methodInvokerFactory = methodInvokerFactory;
            _eventDispatcher = eventDispatchers.FirstOrDefault();
            _idGenerator = idGenerator;
            _transitionCommitter = transitionCommitter;
            _routineCompletionNotifier = routineCompletionNotifier;
            _intentPreprocessor = new AggregateHttpIntentPreprocessor(intentPreprocessors);
            _transitionActions = transitionActions;
            _transitionUserContext = transitionUserContext;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;

            _dasyncJsonSerializer = serializerFactorySelector.Select("dasync+json").Create();

            JsonSettings.Converters.Add(new EntityProjectionConverter(communicationModel));
        }

        public async Task HandleAsync(PathString basePath, HttpContext context, CancellationToken ct)
        {
            var headers = context.Response.Headers;
            headers.Add(DasyncHttpHeaders.PoweredBy, "D-ASYNC");

            var basePathSegmentCount = basePath.Value.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
            var pathSegments = context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(basePathSegmentCount).ToArray();

            if (pathSegments.Length == 0 && context.Request.Query.ContainsKey("react"))
            {
                await HandleEventReactionAsync(null, context, ct);
                return;
            }

            if (pathSegments.Length == 0)
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Empty request URL"));
                return;
            }

            var serviceName = pathSegments[0];
            var serviceDefinition = _communicationModel.FindServiceByName(serviceName);
            // Convenience resolution if a person typed the "Service" suffix where the full service name matches the class name.
            if (serviceDefinition == null && serviceName.EndsWith("Service", StringComparison.OrdinalIgnoreCase))
            {
                var candidateServiceName = serviceName.Substring(0, serviceName.Length - 7);
                serviceDefinition = _communicationModel.FindServiceByName(candidateServiceName);
                if (serviceDefinition != null && serviceDefinition.Implementation != null &&
                    serviceDefinition.Implementation.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                {
                    serviceName = candidateServiceName;
                }
                else
                {
                    serviceDefinition = null;
                }
            }
            if (serviceDefinition == null)
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"Service '{serviceName}' is not registered"));
                return;
            }

            if (serviceDefinition.Type == ServiceType.External)
            {
                context.Response.StatusCode = 403; // Forbidden
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"Cannot invoke external service '{serviceName}' on your behalf"));
                return;
            }

            if (_eventDispatcher != null && pathSegments.Length == 1 && context.Request.Query.ContainsKey("react"))
            {
                await HandleEventReactionAsync(serviceDefinition, context, ct);
                return;
            }

            if (pathSegments.Length == 1)
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("The request URL does not contain a service method"));
                return;
            }

            if (_eventDispatcher != null && context.Request.Query.ContainsKey("subscribe"))
            {
                await HandleEventSubsciptionAsync(serviceDefinition, pathSegments[1], context, ct);
                return;
            }

            var methodName = pathSegments[1];

            if (pathSegments.Length == 3)
            {
                await HandleResultPoll(context, serviceName, methodName, intentId: pathSegments[2]);
                return;
            }

            if (pathSegments.Length > 3)
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("The request URL contains extra segments"));
                return;
            }

            var isQueryRequest = context.Request.Method == "GET";
            var isCommandRequest = context.Request.Method == "PUT" || context.Request.Method == "POST";
            var contentType = context.Request.GetContentType();
            var isJsonRequest = string.Equals(contentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase);
            var isDasyncJsonRequest = string.Equals(contentType.MediaType, "application/dasync+json", StringComparison.OrdinalIgnoreCase);

            if (!isQueryRequest && !isJsonRequest && !isDasyncJsonRequest)
            {
                context.Response.StatusCode = 406; // Not Acceptable
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("The request content type is either not specified or not supported"));
                return;
            }

            if (!isQueryRequest && !isCommandRequest)
            {
                context.Response.StatusCode = 405; // Method Not Allowed
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("The service method invocation must use one of these HTTP verbs: GET, POST, PUT"));
                return;
            }

            var methodId = new RoutineMethodId
            {
                Name = methodName
            };

            if (!_methodResolver.TryResolve(serviceDefinition, methodId, out var methodReference))
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"The service '{serviceDefinition.Name}' does not have method '{methodName}'"));
                return;
            }

#warning Use model to determine if the method is command or query
            var isQueryMethod = GetWordAndSynonyms.Contains(GetFirstWord(methodName));

            if (isQueryRequest && !isQueryMethod)
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"The method '{methodName}' of service '{serviceDefinition.Name}' is a command, but not a query, thus must be invoked with POST or PUT verb"));
                return;
            }

            string parametersJson = null;
            ContinuationDescriptor continuationDescriptor = null;

            if (isQueryRequest)
            {
                var inputObject = new JObject();

                foreach (var kvPair in context.Request.Query)
                {
                    if (kvPair.Value.Count == 0)
                        continue;

                    var parameterName = kvPair.Key;

                    if (kvPair.Value.Count == 1)
                    {
                        var parameterValue = kvPair.Value[0];
                        inputObject.Add(parameterName, parameterValue);
                    }
                    else
                    {
                        var values = new JArray();
                        foreach (var value in kvPair.Value)
                            values.Add(value);
                        inputObject.Add(parameterName, values);
                    }
                }

                parametersJson = inputObject.ToString();
            }
            else if (isJsonRequest)
            {
                parametersJson = await context.Request.ReadBodyAsStringAsync(contentType);
            }
            else
            {
                var inputJson = await context.Request.ReadBodyAsStringAsync(contentType);
                var envelope = JsonConvert.DeserializeObject<CommandEnvelope>(inputJson, JsonSettings);
                parametersJson = envelope.Parameters;
                continuationDescriptor = envelope.Continuation;
            }

            var methodInvoker = _methodInvokerFactory.Create(methodReference.Definition.MethodInfo);
            var parameterContainer = methodInvoker.CreateParametersContainer();

            if (!string.IsNullOrWhiteSpace(parametersJson))
            {
                if (isDasyncJsonRequest)
                {
                    _dasyncJsonSerializer.Populate(parametersJson, parameterContainer);
                }
                else
                {
                    JsonConvert.PopulateObject(parametersJson, parameterContainer, JsonSettings);
                }
            }

            var externalRequestId = context.Request.Headers.TryGetValue(DasyncHttpHeaders.RequestId, out var requestIdValues) && requestIdValues.Count > 0 ? requestIdValues[0] : null;
            var externalCorrelationId = context.Request.Headers.TryGetValue(DasyncHttpHeaders.CorrelationId, out var correlationIdValues) && correlationIdValues.Count > 0 ? correlationIdValues[0] : null;

            var rfc7240Preferences = GetPreferences(context.Request.Headers);

            var isHttpRequestBlockingExecution = !(rfc7240Preferences.RespondAsync == true);

            _transitionUserContext.Current = GetUserContext(context);

            _intentPreprocessor.PrepareContext(context);
            if (await _intentPreprocessor.PreprocessAsync(context, serviceDefinition, methodId, parameterContainer).ConfigureAwait(false))
                return;

            var serviceId = new ServiceId { Name = serviceDefinition.Name };
            var intentId = _idGenerator.NewId();

            if (isQueryMethod)
            {
                var serviceInstance = _domainServiceProvider.GetService(serviceDefinition.Implementation);

                foreach (var postAction in _transitionActions)
                    await postAction.OnRoutineStartAsync(serviceDefinition, serviceId, methodId, intentId);

                Task task;
                try
                {
                    task = methodInvoker.Invoke(serviceInstance, parameterContainer);
                    if (methodReference.Definition.MethodInfo.ReturnType != typeof(void))
                    {
                        try { await task; } catch { }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException)
                        ex = ex.InnerException;
                    task = Task.FromException(ex);
                }
                var taskResult = task?.ToTaskResult() ?? new TaskResult();

                foreach (var postAction in _transitionActions)
                    await postAction.OnRoutineCompleteAsync(serviceDefinition, serviceId, methodId, intentId, taskResult);

                await RespondWithRoutineResult(context, taskResult, isDasyncJsonRequest);
            }
            else
            {
                var intent = new ExecuteRoutineIntent
                {
                    Id = intentId,
                    ServiceId = new ServiceId { Name = serviceDefinition.Name },
                    MethodId = methodId,
                    Parameters = parameterContainer,
                    Continuation = continuationDescriptor
                };

                var actions = new ScheduledActions
                {
                    ExecuteRoutineIntents = new List<ExecuteRoutineIntent> { intent }
                };

                var options = new TransitionCommitOptions
                {
                    NotifyOnRoutineCompletion = isHttpRequestBlockingExecution,
                    RequestId = externalRequestId,
                    CorrelationId = externalCorrelationId ?? externalRequestId
                };

                await _transitionCommitter.CommitAsync(actions, null, options, default);

                var waitTime = rfc7240Preferences.Wait;
                if (isHttpRequestBlockingExecution || waitTime > MaxLongPollTime)
                    waitTime = MaxLongPollTime;

                if (waitTime > TimeSpan.Zero)
                {
                    var cts = new CancellationTokenSource();
                    var completionSink = new TaskCompletionSource<TaskResult>();
                    _routineCompletionNotifier.NotifyCompletion(intent.ServiceId, intent.MethodId, intent.Id, completionSink, cts.Token);
                    try
                    {
                        var taskResult = await completionSink.Task.WithTimeout(waitTime);
                        await RespondWithRoutineResult(context, taskResult, isDasyncJsonRequest);
                        return;
                    }
                    catch (TaskCanceledException)
                    {
                        cts.Cancel();
                    }
                }

                var location = context.Request.Path.ToString() + "/" + intent.Id;
                context.Response.Headers.Add("Location", location);
                context.Response.StatusCode = DasyncHttpCodes.Scheduled;
                return;
            }
        }

        private async Task HandleResultPoll(HttpContext context, string serviceName, string methodName, string intentId)
        {
            var rfc7240Preferences = GetPreferences(context.Request.Headers);

            var serviceId = new ServiceId
            {
                Name = serviceName
            };

            var methodId = new RoutineMethodId
            {
                Name = methodName
            };

            TaskResult taskResult = null;

            var waitTime = rfc7240Preferences.Wait;
            if (waitTime > MaxLongPollTime)
                waitTime = MaxLongPollTime;

            if (waitTime <= TimeSpan.Zero)
            {
                taskResult = await _routineCompletionNotifier.TryPollCompletionAsync(serviceId, methodId, intentId, default);
            }
            else
            {
                var cts = new CancellationTokenSource();
                try
                {
                    var completionSink = new TaskCompletionSource<TaskResult>();
                    _routineCompletionNotifier.NotifyCompletion(serviceId, methodId, intentId, completionSink, cts.Token);
                    taskResult = await completionSink.Task.WithTimeout(waitTime);
                }
                catch (TaskCanceledException)
                {
                    cts.Cancel();
                }
            }

            if (taskResult == null)
            {
                context.Response.Headers.Add("Date", DateTimeOffset.Now.ToString("u"));
                context.Response.StatusCode = 304; // Not Modified
            }
            else
            {
                var contentType = context.Request.GetContentType();
                var isDasyncJsonRequest = string.Equals(contentType.MediaType, "application/dasync+json", StringComparison.OrdinalIgnoreCase);
                await RespondWithRoutineResult(context, taskResult, isDasyncJsonRequest);
            }
        }

        private async Task RespondWithRoutineResult(HttpContext context, TaskResult taskResult, bool isDasyncJsonRequest)
        {
            if (taskResult.IsCanceled)
            {
                context.Response.StatusCode = DasyncHttpCodes.Canceled;

                if (isDasyncJsonRequest)
                {
                    context.Response.ContentType = "application/dasync+json";
                    _dasyncJsonSerializer.Serialize(context.Response.Body, taskResult);
                }

                return;
            }
            else if (taskResult.IsFaulted)
            {
                context.Response.StatusCode = DasyncHttpCodes.Faulted;

                if (isDasyncJsonRequest)
                {
                    context.Response.ContentType = "application/dasync+json";
                    _dasyncJsonSerializer.Serialize(context.Response.Body, taskResult);
                }
                else
                {
                    context.Response.ContentType = "application/json";
                    var errorEnvelope = new ErrorEnvelope
                    {
                        Error = ExceptionToErrorConverter.Convert(taskResult.Exception)
                    };
                    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(errorEnvelope, JsonSettings)));
                }

                return;
            }
            else
            {
                context.Response.StatusCode = DasyncHttpCodes.Succeeded;

                if (isDasyncJsonRequest)
                {
                    context.Response.ContentType = "application/dasync+json";
                    _dasyncJsonSerializer.Serialize(context.Response.Body, taskResult);
                }
                else
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskResult.Value, JsonSettings)));
                }

                return;
            }
        }

        private async Task HandleEventSubsciptionAsync(IServiceDefinition serviceDefinition, string eventName, HttpContext context, CancellationToken ct)
        {
            if (context.Request.Method != "PUT" && context.Request.Method != "POST")
            {
                context.Response.StatusCode = 405; // Method Not Allowed
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("To subscribe to an event, use one of these HTTP verbs: GET, POST, PUT"));
                return;
            }

            var serviceName = (context.Request.Query.TryGetValue("service", out var serviceValues) && serviceValues.Count == 1) ? serviceValues[0] : null;

            if (string.IsNullOrWhiteSpace(serviceName))
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Missing 'service=xxx' in the URL query."));
                return;
            }

            var proxyName = (context.Request.Query.TryGetValue("proxy", out var proxyValues) && proxyValues.Count == 1) ? proxyValues[0] : null;

            var subscriberServiceId = new ServiceId { Name = serviceName, Proxy = proxyName };

            var eventId = new EventId { EventName = eventName };
            var publisherServiceId = new ServiceId { Name = serviceDefinition.Name };
            var eventDesc = new EventDescriptor { ServiceId = publisherServiceId, EventId = eventId };

            _eventDispatcher.OnSubscriberAdded(eventDesc, subscriberServiceId);

            context.Response.StatusCode = 200;
        }

        private async Task HandleEventReactionAsync(IServiceDefinition serviceDefinitionFilter, HttpContext context, CancellationToken ct)
        {
            if (context.Request.Method != "PUT" && context.Request.Method != "POST")
            {
                context.Response.StatusCode = 405; // Method Not Allowed
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("To invoke a reaction to an event, use one of these HTTP verbs: GET, POST, PUT"));
                return;
            }

            var eventName = (context.Request.Query.TryGetValue("event", out var eventValues) && eventValues.Count == 1) ? eventValues[0] : null;
            if (string.IsNullOrWhiteSpace(eventName))
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Missing 'event=xxx' in the URL query."));
                return;
            }

            var serviceName = (context.Request.Query.TryGetValue("service", out var serviceValues) && serviceValues.Count == 1) ? serviceValues[0] : null;
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Missing 'service=xxx' in the URL query."));
                return;
            }

            var eventDesc = new EventDescriptor
            {
                EventId = new EventId { EventName = eventName },
                ServiceId = new ServiceId { Name = serviceName }
            };

            var eventHandlers = _eventDispatcher.GetEventHandlers(eventDesc);
            if ((eventHandlers?.Count ?? 0) == 0)
            {
                context.Response.StatusCode = DasyncHttpCodes.Succeeded;
                return;
            }

            var contentType = context.Request.GetContentType();
            var isJsonRequest = string.Equals(contentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase);
            var isDasyncJsonRequest = string.Equals(contentType.MediaType, "application/dasync+json", StringComparison.OrdinalIgnoreCase);

            if (!isJsonRequest && !isDasyncJsonRequest)
            {
                context.Response.StatusCode = 406; // Not Acceptable
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("The request content type is either not specified or not supported"));
                return;
            }

            string parametersJson = null;
            if (isJsonRequest)
            {
                parametersJson = await context.Request.ReadBodyAsStringAsync(contentType);
            }
            else if (isDasyncJsonRequest)
            {
                var envelopeJson = await context.Request.ReadBodyAsStringAsync(contentType);
                var envelope = JsonConvert.DeserializeObject<EventEnvelope>(envelopeJson, JsonSettings);
                parametersJson = envelope.Parameters;
            }

            var results = new List<RaiseEventResult>();

            _transitionUserContext.Current = GetUserContext(context);

            foreach (var subscriber in eventHandlers)
            {
                if (serviceDefinitionFilter != null && !string.Equals(subscriber.ServiceId.Name, serviceDefinitionFilter.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var subscriberServiceDefinition = _communicationModel.FindServiceByName(subscriber.ServiceId.Name);

                var eventHandlerRoutineMethodId = new RoutineMethodId
                {
                    Name = subscriber.MethodId.Name
                };
                var methodReference = _methodResolver.Resolve(subscriberServiceDefinition, eventHandlerRoutineMethodId);

                var methodInvoker = _methodInvokerFactory.Create(methodReference.Definition.MethodInfo);
                var parameterContainer = methodInvoker.CreateParametersContainer();

                if (!string.IsNullOrWhiteSpace(parametersJson))
                {
                    if (isDasyncJsonRequest)
                    {
                        _dasyncJsonSerializer.Populate(parametersJson, parameterContainer);
                    }
                    else
                    {
                        JsonConvert.PopulateObject(parametersJson, parameterContainer, JsonSettings);
                    }
                }

                var intentId = _idGenerator.NewId();

                _intentPreprocessor.PrepareContext(context);
                if (await _intentPreprocessor.PreprocessAsync(context, subscriberServiceDefinition, subscriber.MethodId, parameterContainer).ConfigureAwait(false))
                    return;

                foreach (var postAction in _transitionActions)
                    await postAction.OnRoutineStartAsync(subscriberServiceDefinition, subscriber.ServiceId, subscriber.MethodId, intentId);

                var serviceInstance = _domainServiceProvider.GetService(subscriberServiceDefinition.Implementation);
                Task task;
                try
                {
                    task = methodInvoker.Invoke(serviceInstance, parameterContainer);
                    if (methodReference.Definition.MethodInfo.ReturnType != typeof(void))
                    {
                        try { await task; } catch { }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException)
                        ex = ex.InnerException;
                    task = Task.FromException(ex);
                }
                var taskResult = task?.ToTaskResult() ?? new TaskResult();

                foreach (var postAction in _transitionActions)
                    await postAction.OnRoutineCompleteAsync(subscriberServiceDefinition, subscriber.ServiceId, subscriber.MethodId, intentId, taskResult);

                results.Add(new RaiseEventResult
                {
                    ServiceName = subscriber.ServiceId.Name,
                    MethodName = subscriber.MethodId.Name,
                    Result = taskResult
                });
            }

            context.Response.StatusCode = DasyncHttpCodes.Succeeded;

            if (isJsonRequest)
            {
                context.Response.ContentType = "application/json";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(results, JsonSettings)));
            }
            else if (isDasyncJsonRequest)
            {
                context.Response.ContentType = "application/dasync+json";
                _dasyncJsonSerializer.Serialize(context.Response.Body, results);
            }
        }

        public struct RaiseEventResult
        {
            public string ServiceName;
            public string MethodName;
            public TaskResult Result;
        }

        private static NameValueCollection GetUserContext(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(DasyncHttpHeaders.Context, out var contextstrings))
                return null;
            var userContext = new NameValueCollection();
            foreach (var contextString in contextstrings)
                userContext = userContext.Load(contextString);
            return userContext;
        }

        private static Route GetSelfRoute(IHeaderDictionary headers)
        {
            var route = new Route();

            route.Region = headers[DasyncHttpHeaders.RouteRegion].FirstOrDefault();
            route.Service = headers[DasyncHttpHeaders.RouteService].FirstOrDefault();
            route.InstanceId = headers[DasyncHttpHeaders.RouteInstanceId].FirstOrDefault();
            route.PartitionKey = headers[DasyncHttpHeaders.RoutePartitionKey].FirstOrDefault();
            route.SequenceKey = headers[DasyncHttpHeaders.RouteSequenceKey].FirstOrDefault();
            route.ApiVersion = headers[DasyncHttpHeaders.RouteApiVersion].FirstOrDefault();

            return route;
        }

        private static Route GetReplyRoute(IHeaderDictionary headers)
        {
            var route = new Route();

            route.Region = headers[DasyncHttpHeaders.ReplyRegion].FirstOrDefault();
            route.Service = headers[DasyncHttpHeaders.ReplyService].FirstOrDefault();
            route.InstanceId = headers[DasyncHttpHeaders.ReplyInstanceId].FirstOrDefault();
            route.PartitionKey = headers[DasyncHttpHeaders.ReplyPartitionKey].FirstOrDefault();
            route.SequenceKey = headers[DasyncHttpHeaders.ReplySequenceKey].FirstOrDefault();
            route.ApiVersion = headers[DasyncHttpHeaders.ReplyApiVersion].FirstOrDefault();

            return route;
        }

        private static string GetFirstWord(string text)
        {
            if (text.Length <= 1)
                return text;

            for (var i = 1; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsUpper(c) || c == '_' || c == '-' || c == '+' || c == '#' || c == '@' || c == '!' || char.IsWhiteSpace(c))
                {
                    return text.Substring(0, i);
                }
            }

            return text;
        }

        private static readonly HashSet<string> GetWordAndSynonyms =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Get",
                "List",
                "Fetch",
                "Retrieve",
                "Collect",
                "Grab",
                "Pick",
                "Peek",
                "Select",
                "Take",
                "Receive",
                "Query",
                "Find",
                "Search"
            };

        public struct RFC7240Preferences
        {
            public bool? RespondAsync;
            public TimeSpan Wait;
        }

        static RFC7240Preferences GetPreferences(IHeaderDictionary headers)
        {
            var preferences = new RFC7240Preferences();

            if (headers.TryGetValue("Prefer", out var values))
            {
                for (var i = 0; i < values.Count; i++)
                {
                    var headerValue = values[i];

                    // Yes, if somebody types e.g. 'Foo#respond-async#Bar' the condition succeeds as well, but that's 'tire kicking'.
                    if (headerValue.Contains("respond-async"))
                        preferences.RespondAsync = true;

                    // Same here.
                    var waitKeywordIndex = headerValue.IndexOf("wait=");
                    if (waitKeywordIndex >= 0)
                    {
                        var waitValueStartIndex = waitKeywordIndex + 5;
                        var waitValueEndIndex = waitValueStartIndex;
                        for (; waitValueEndIndex < headerValue.Length; waitValueEndIndex++)
                        {
                            if (!char.IsDigit(headerValue[waitValueEndIndex]))
                                break;
                        }
                        if (waitValueStartIndex != waitValueEndIndex &&
                            int.TryParse(headerValue.Substring(
                                waitValueStartIndex, waitValueEndIndex - waitValueStartIndex),
                                out var waitSeconds))
                        {
                            preferences.Wait = TimeSpan.FromSeconds(waitSeconds);
                        }
                    }
                }
            }

            return preferences;
        }

        // Prefer: respond-async, wait=10
    }

    internal static class HttpRequestExtensions
    {
        private static readonly ContentType UnknownContentType = new ContentType();

        public static ContentType GetContentType(this HttpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ContentType))
                return UnknownContentType;
            return new ContentType(request.ContentType);
        }

        public static async Task<string> ReadBodyAsStringAsync(this HttpRequest request, ContentType contentType = null)
        {
#warning TODO: use content encoding for the charset
            //if (contentType == null)
            //    contentType = request.GetContentType();

            return await new StreamReader(request.Body).ReadToEndAsync();
        }
    }
}
