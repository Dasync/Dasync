using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Communication.Http;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Engine;
using Dasync.EETypes.Platform;
using Dasync.EETypes.Resolvers;
using Dasync.Hosting.AspNetCore.Errors;
using Dasync.Hosting.AspNetCore.Http;
using Dasync.Hosting.AspNetCore.Invocation;
using Dasync.Hosting.AspNetCore.Utils;
using Dasync.Modeling;
using Dasync.Serialization;
using Dasync.ValueContainer;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Dasync.Hosting.AspNetCore
{
    public interface IHttpRequestHandler
    {
        Task HandleAsync(PathString basePath, HttpContext context, CancellationToken ct);
    }

    public class HttpRequestHandler : IHttpRequestHandler
    {
        private readonly ISerializerProvider _serializerProvider;
        private readonly ISerializer _jsonSerializer;
        private readonly IUniqueIdGenerator _idGenerator;
        private readonly IRoutineCompletionNotifier _routineCompletionNotifier;
        private readonly IServiceResolver _serviceResolver;
        private readonly IMethodResolver _methodResolver;
        private readonly ILocalMethodRunner _localTransitionRunner;

        private TimeSpan MaxLongPollTime = TimeSpan.FromMinutes(2);

        public HttpRequestHandler(
            ISerializerProvider serializerProvider,
            IUniqueIdGenerator idGenerator,
            IRoutineCompletionNotifier routineCompletionNotifier,
            IServiceResolver serviceResolver,
            IMethodResolver methodResolver,
            ILocalMethodRunner localTransitionRunner)
        {
            _idGenerator = idGenerator;
            _routineCompletionNotifier = routineCompletionNotifier;
            _serviceResolver = serviceResolver;
            _methodResolver = methodResolver;
            _localTransitionRunner = localTransitionRunner;

            _serializerProvider = serializerProvider;
            _jsonSerializer = _serializerProvider.GetSerializer("json");
        }

        public async Task HandleAsync(PathString basePath, HttpContext context, CancellationToken ct)
        {
            var isQueryRequest = context.Request.Method == "GET";
            var isCommandRequest = context.Request.Method == "POST";

            if (!isQueryRequest && !isCommandRequest)
            {
                await ReplyWithTextError(context.Response, 405, "Only GET and POST verbs are allowed");
                return;
            }

            var headers = context.Response.Headers;
            headers.Add(DasyncHttpHeaders.PoweredBy, "D-ASYNC");

            var basePathSegmentCount = basePath.Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var pathSegments = context.Request.Path.Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(basePathSegmentCount).ToArray();

            if (pathSegments.Length == 0)
            {
                await ReplyWithTextError(context.Response, 404, "Empty request URL");
                return;
            }

            var serviceId = new ServiceId { Name = pathSegments[0] };
            if (!_serviceResolver.TryResolve(serviceId, out var serviceReference))
            {
                await ReplyWithTextError(context.Response, 404, $"Service '{serviceId.Name}' is not registered");
                return;
            }

            if (serviceReference.Definition.Type == ServiceType.External)
            {
                await ReplyWithTextError(context.Response, 404, $"Cannot invoke external service '{serviceReference.Definition.Name}' on your behalf");
                return;
            }

            if (pathSegments.Length == 1)
            {
                await ReplyWithTextError(context.Response, 404, "The request URL does not contain a service method");
                return;
            }

            var methodId = new MethodId { Name = pathSegments[1] };
            string methodIntentId = null;

            if (pathSegments.Length == 3)
            {
                methodIntentId = pathSegments[2];

                if (isQueryRequest)
                {
                    await HandleResultPoll(context, serviceReference.Id, methodId, methodIntentId);
                    return;
                }
            }

            if (pathSegments.Length > 3)
            {
                await ReplyWithTextError(context.Response, 404, "The request URL contains extra segments");
                return;
            }

            var contentType = context.Request.GetContentType();
            ISerializer serializer;
            try
            {
                serializer = GetSerializer(contentType, isQueryRequest);
            }
            catch (ArgumentException)
            {
                await ReplyWithTextError(context.Response, 406, $"The Content-Type '{contentType.MediaType}' is not supported");
                return;
            }

            if (!_methodResolver.TryResolve(serviceReference.Definition, methodId, out var methodReference))
            {
                await ReplyWithTextError(context.Response, 404, $"The service '{serviceReference.Definition.Name}' does not have method '{methodId.Name}'");
                return;
            }

            if (isQueryRequest && !methodReference.Definition.IsQuery)
            {
                await ReplyWithTextError(context.Response, 404, $"The method '{methodId.Name}' of service '{serviceReference.Definition.Name}' is a command, but not a query, thus must be invoked with the POST verb");
                return;
            }

            MethodInvocationData invokeData = null;
            MethodContinuationData continueData = null;
            IValueContainer parametersContainer;
            bool compressResponse = false;
            bool respondWithEnvelope = false;

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

                var parametersJson = inputObject.ToString();

                parametersContainer = new SerializedValueContainer(parametersJson, _jsonSerializer);

                invokeData = new MethodInvocationData
                {
                    Service = serviceId,
                    Method = methodId,
                    Parameters = parametersContainer,
                    Caller = GetCaller(context.Request.Headers)
                };
            }
            else
            {
                var payloadStream = context.Request.Body;

                var encoding = context.Request.GetContentEncoding();
                if (!string.IsNullOrEmpty(encoding))
                {
                    if ("gzip".Equals(encoding, StringComparison.OrdinalIgnoreCase))
                    {
                        compressResponse = true;
                        payloadStream = new GZipStream(payloadStream, CompressionMode.Decompress, leaveOpen: true);
                    }
                    else if ("deflate".Equals(encoding, StringComparison.OrdinalIgnoreCase))
                    {
                        payloadStream = new DeflateStream(payloadStream, CompressionMode.Decompress, leaveOpen: true);
                    }
                    else
                    {
                        await ReplyWithTextError(context.Response, 406, $"The Content-Encoding '{encoding}' is not supported");
                        return;
                    }
                }

                var envelopeType = context.Request.Headers.GetValue(DasyncHttpHeaders.Envelope);

                if (string.IsNullOrEmpty(envelopeType))
                {
                    var payload = await payloadStream.ToBytesAsync();
                    parametersContainer = new SerializedValueContainer(payload, serializer);

                    if (string.IsNullOrEmpty(methodIntentId))
                    {
                        invokeData = new MethodInvocationData
                        {
                            Service = serviceId,
                            Method = methodId,
                            Parameters = parametersContainer,
                            Caller = GetCaller(context.Request.Headers)
                        };
                    }
                    else
                    {
                        continueData = new MethodContinuationData
                        {
                            Service = serviceId,
                            Method = methodId.CopyTo(new PersistedMethodId()),
                            Result = parametersContainer,
                            Caller = GetCaller(context.Request.Headers)
                        };
                        continueData.Method.IntentId = methodIntentId;
                        // TODO: get ETag from the query string
                    }
                }
                else if (envelopeType.Equals("invoke", StringComparison.OrdinalIgnoreCase))
                {
                    respondWithEnvelope = true;
                    invokeData = serializer.Deserialize<MethodInvocationData>(payloadStream);
                }
                else if (envelopeType.Equals("continue", StringComparison.OrdinalIgnoreCase))
                {
                    respondWithEnvelope = true;
                    continueData = serializer.Deserialize<MethodContinuationData>(payloadStream);
                }
                else
                {
                    await ReplyWithTextError(context.Response, 406, $"Unknown envelope type '{envelopeType}'");
                    return;
                }
            }

            var intentId = context.Request.Headers.GetValue(DasyncHttpHeaders.IntentId) ?? _idGenerator.NewId();
            var externalRequestId = context.Request.Headers.GetValue(DasyncHttpHeaders.RequestId);
            var externalCorrelationId = context.Request.Headers.GetValue(DasyncHttpHeaders.CorrelationId);
            var isRetry = context.Request.Headers.IsRetry();

            var rfc7240Preferences = context.Request.Headers.GetRFC7240Preferences();
            var isHttpRequestBlockingExecution = !(rfc7240Preferences.RespondAsync == true);
            var waitTime = rfc7240Preferences.Wait;
            if (waitTime > MaxLongPollTime)
                waitTime = MaxLongPollTime;
            var waitForResult = isHttpRequestBlockingExecution || waitTime > TimeSpan.Zero;

            var communicatorMessage = new HttpCommunicatorMessage
            {
                IsRetry = isRetry,
                RequestId = externalRequestId,
                WaitForResult = waitForResult
            };

            if (invokeData != null)
            {
                if (invokeData.IntentId == null)
                    invokeData.IntentId = intentId;

                var invokeTask = _localTransitionRunner.RunAsync(invokeData, communicatorMessage);

                if (isHttpRequestBlockingExecution)
                {
                    var invocationResult = await invokeTask;

                    if (invocationResult.Outcome == InvocationOutcome.Complete)
                    {
                        await RespondWithMethodResult(context.Response, invocationResult.Result, serializer, respondWithEnvelope, compressResponse);
                        return;
                    }

                    if (waitForResult)
                    {
                        var taskResult = await TryWaitForResultAsync(
                            serviceReference.Id,
                            methodId, intentId, waitTime);

                        if (taskResult != null)
                        {
                            await RespondWithMethodResult(context.Response, taskResult, serializer, respondWithEnvelope, compressResponse);
                            return;
                        }
                    }
                }
                else
                {
                    // TODO: continue 'invokeTask' and handle exceptions in background
                }

                communicatorMessage.WaitForResult = false;

                var location = string.Concat(context.Request.Path, "/", intentId);
                context.Response.Headers.Add("Location", location);
                context.Response.Headers.Add(DasyncHttpHeaders.IntentId, intentId);
                context.Response.StatusCode = DasyncHttpCodes.Scheduled;
            }
            else if (continueData != null)
            {
                if (continueData.IntentId == null)
                    continueData.IntentId = intentId;

                var continueTask = _localTransitionRunner.ContinueAsync(continueData, communicatorMessage);
                // TODO: continue 'continueTask' in backgraound to handle exceptions

                context.Response.Headers.Add("Location", context.Request.Path.ToString());
                context.Response.Headers.Add(DasyncHttpHeaders.IntentId, continueData.Method.IntentId);
                context.Response.StatusCode = DasyncHttpCodes.Scheduled;
            }
        }

        private async Task HandleResultPoll(HttpContext context, ServiceId serviceId, MethodId methodId, string intentId)
        {
            var rfc7240Preferences = context.Request.Headers.GetRFC7240Preferences();

            ITaskResult taskResult = null;

            var waitTime = rfc7240Preferences.Wait;
            if (waitTime > MaxLongPollTime)
                waitTime = MaxLongPollTime;

            if (waitTime <= TimeSpan.Zero)
            {
                taskResult = await _routineCompletionNotifier.TryPollCompletionAsync(serviceId, methodId, intentId, default);
            }
            else
            {
                taskResult = await TryWaitForResultAsync(serviceId, methodId, intentId, waitTime);
            }

            if (taskResult == null)
            {
                context.Response.Headers.Add("Date", DateTimeOffset.Now.ToString("u"));
                context.Response.StatusCode = DasyncHttpCodes.Running;
            }
            else
            {
                var compress = context.Request.Headers.ContainsValue("Accept-Encoding", "gzip");
                var useEnvelope = context.Request.Headers.TryGetValue(DasyncHttpHeaders.Envelope, out _);
                var serializer = GetSerializer(context.Request.GetContentType(), isQueryRequest: true);
                await RespondWithMethodResult(context.Response, taskResult, serializer, useEnvelope, compress);
            }
        }

        private async Task<ITaskResult> TryWaitForResultAsync(
            ServiceId serviceId, MethodId routineMethodId, string intentId, TimeSpan? waitTime)
        {
            var cts = new CancellationTokenSource();
            var completionSink = new TaskCompletionSource<ITaskResult>();

            var trackingToken = _routineCompletionNotifier.NotifyOnCompletion(
                serviceId, routineMethodId, intentId, completionSink, cts.Token);

            ITaskResult result;
            try
            {
                result = await completionSink.WithTimeout(waitTime).Task
                    .ContinueWith(t => t.IsCanceled ? null : t.Result);
            }
            catch (TaskCanceledException)
            {
                result = null;
            }
            finally
            {
                cts.Cancel();
                _routineCompletionNotifier.StopTracking(trackingToken);
            }

            return result;
        }

        private async Task RespondWithMethodResult(HttpResponse response, ITaskResult taskResult, ISerializer serializer, bool useEnvelope, bool compress)
        {
            if (taskResult.IsCanceled)
            {
                response.StatusCode = DasyncHttpCodes.Canceled;
                response.Headers.Add(DasyncHttpHeaders.TaskResult, "canceled");
            }
            else if (taskResult.IsFaulted())
            {
                response.StatusCode = DasyncHttpCodes.Faulted;
                response.Headers.Add(DasyncHttpHeaders.TaskResult, "faulted");
            }
            else
            {
                response.StatusCode = DasyncHttpCodes.Succeeded;
                response.Headers.Add(DasyncHttpHeaders.TaskResult, "succeeded");
            }

            var responseStream = response.Body;
            if (compress)
            {
                response.Headers.Add("Content-Encoding", "gzip");
                responseStream = new GZipStream(responseStream, CompressionLevel.Optimal, leaveOpen: true);
            }

            response.ContentType = "application/" + serializer.Format;

            if (useEnvelope)
            {
                response.Headers.Add(DasyncHttpHeaders.Envelope, "result");
                serializer.Serialize(responseStream, taskResult);
            }
            else
            {
                if (taskResult.IsFaulted())
                {
                    // TODO: allow custom transformer
                    var errorEnvelope = new ErrorEnvelope
                    {
                        Error = taskResult.Exception.ToError()
                    };
                    serializer.Serialize(responseStream, errorEnvelope);
                }
                else if (taskResult.IsSucceeded())
                {
                    if (taskResult.Value != null)
                        serializer.Serialize(responseStream, taskResult.Value);
                }
            }

            if (compress)
                responseStream.Dispose();
        }

        private Task ReplyWithTextError(HttpResponse response, int statusCode, string text)
        {
            response.StatusCode = statusCode;
            response.ContentType = "text/plain";
            return response.Body.WriteUtf8StringAsync(text);
        }

        private ISerializer GetSerializer(ContentType contentType, bool isQueryRequest)
        {
            if (contentType == null ||
                string.IsNullOrEmpty(contentType.MediaType) ||
                string.Equals(contentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
                (isQueryRequest && string.Equals(contentType.MediaType, "application/octet-stream", StringComparison.OrdinalIgnoreCase)))
            {
                return _jsonSerializer; // default serializer
            }
            else
            {
                var format =
                    contentType.MediaType.StartsWith("application/", StringComparison.OrdinalIgnoreCase)
                    ? contentType.MediaType.Substring(12)
                    : contentType.MediaType;

                return _serializerProvider.GetSerializer(format);
            }
        }

        private CallerDescriptor GetCaller(IHeaderDictionary headers)
        {
            CallerDescriptor caller = null;
            var callerServiceName = headers.GetValue(DasyncHttpHeaders.CallerServiceName);
            var callerServiceProxy = headers.GetValue(DasyncHttpHeaders.CallerServiceProxy);
            if (!string.IsNullOrEmpty(callerServiceName))
            {
                caller = new CallerDescriptor
                {
                    IntentId = headers.GetValue(DasyncHttpHeaders.CallerIntentId),
                    Service = new ServiceId { Name = callerServiceName, Proxy = callerServiceProxy }
                };

                var callerMethodName = headers.GetValue(DasyncHttpHeaders.CallerMethodName);
                if (!string.IsNullOrEmpty(callerServiceName))
                {
                    caller.Method = new MethodId { Name = callerMethodName };
                }

                var callerEventName = headers.GetValue(DasyncHttpHeaders.CallerEventName);
                if (!string.IsNullOrEmpty(callerEventName))
                {
                    caller.Event = new EventId { Name = callerEventName };
                }
            }
            return caller;
        }
    }
}
