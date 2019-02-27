using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Ioc;
using Dasync.ExecutionEngine;
using Dasync.ExecutionEngine.Extensions;
using Dasync.Json;
using Dasync.Modeling;
using Dasync.Proxy;
using Dasync.Serialization;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DasyncAspNetCore
{
    public interface IHttpRequestHandler
    {
        Task HandleAsync(PathString basePath, HttpContext context, CancellationToken ct);
    }

    public class HttpRequestHandler : IHttpRequestHandler
    {
        private readonly ICommunicationModelProvider _communicationModelProvider;
        private readonly IDomainServiceProvider _domainServiceProvider;
        private readonly IRoutineMethodResolver _routineMethodResolver;
        private readonly IMethodInvokerFactory _methodInvokerFactory;
        private readonly ISerializer _dasyncJsonSerializer;

        public HttpRequestHandler(
            ICommunicationModelProvider communicationModelProvider,
            IDomainServiceProvider domainServiceProvider,
            IRoutineMethodResolver routineMethodResolver,
            IMethodInvokerFactory methodInvokerFactory,
            ISerializerFactorySelector serializerFactorySelector)
        {
            _communicationModelProvider = communicationModelProvider;
            _domainServiceProvider = domainServiceProvider;
            _routineMethodResolver = routineMethodResolver;
            _methodInvokerFactory = methodInvokerFactory;

            _dasyncJsonSerializer = serializerFactorySelector.Select("dasync+json").Create();
        }

        public async Task HandleAsync(PathString basePath, HttpContext context, CancellationToken ct)
        {
            var headers = context.Response.Headers;
            headers.Add(DasyncHttpHeaders.PoweredBy, "D-ASYNC");

            var basePathSegmentCount = basePath.Value.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
            var pathSegments = context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(basePathSegmentCount).ToArray();

            if (pathSegments.Length == 0)
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("Empty request URL"));
                return;
            }

            var serviceName = pathSegments[0];
            var serviceDefinition = _communicationModelProvider.Model.FindServiceByName(serviceName);
            if (serviceDefinition == null)
            {
                context.Response.StatusCode = 400;
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

            if (pathSegments.Length == 1)
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("The request URL does not contain a service method"));
                return;
            }

            if (pathSegments.Length > 2)
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("The request URL contains extra segments"));
                return;
            }

            var isQueryRequest = context.Request.Method == "GET";
            var isCommandRequest = context.Request.Method == "PUT" || context.Request.Method == "POST";
            var isJsonRequest = string.Equals(context.Request.ContentType, "application/json", StringComparison.OrdinalIgnoreCase);
            var isDasyncJsonRequest = string.Equals(context.Request.ContentType, "application/dasync+json", StringComparison.OrdinalIgnoreCase);

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

            var methodName = pathSegments[1];

            var routineMethodId = new RoutineMethodId
            {
                MethodName = methodName
            };

#warning Use model for method resolution instead
            MethodInfo routineMethod;
            try
            {
                routineMethod = _routineMethodResolver.Resolve(serviceDefinition.Implementation, routineMethodId);
            }
            catch
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"The service '{serviceDefinition.Name}' does not have method '{methodName}'"));
                return;
            }

#warning Use model to determine if the method is command or query
            var isQueryMethod = GetWordAndSynonyms.Contains(GetFirstWord(methodName));

            if (isQueryRequest && !isQueryMethod)
            {
                context.Response.StatusCode = 400;
                context.Response.ContentType = "text/plain";
                await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"The method '{methodName}' of service '{serviceDefinition.Name}' is a command, but not a query, thus must be invoked with POST or PUT verb"));
                return;
            }

            string parametersJson = null;

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
                parametersJson = await new StreamReader(context.Request.Body).ReadToEndAsync();
            }
            else
            {
                var inputJson = await new StreamReader(context.Request.Body).ReadToEndAsync();
                var envelope = JsonConvert.DeserializeObject<CommandEnvelope>(inputJson);
                parametersJson = envelope.Parameters;
            }

            var methodInvoker = _methodInvokerFactory.Create(routineMethod);
            var parameterContainer = methodInvoker.CreateParametersContainer();

            if (!string.IsNullOrEmpty(parametersJson))
            {
                if (isDasyncJsonRequest)
                {
                    _dasyncJsonSerializer.Populate(parametersJson, parameterContainer);
                }
                else
                {
                    JsonConvert.PopulateObject(parametersJson, parameterContainer);
                }
            }

            var rfc7240Preferences = GetPreferences(context.Request.Headers);

            var isHttpRequestBlockingExecution = !(rfc7240Preferences.RespondAsync == true);

            if (isHttpRequestBlockingExecution)
            {
                var serviceInstance = _domainServiceProvider.GetService(serviceDefinition.Implementation);
                var task = methodInvoker.Invoke(serviceInstance, parameterContainer);
                try { await task; } catch { }
                var taskResult = task.ToTaskResult();

                if (taskResult.IsCanceled)
                {
                    context.Response.StatusCode = 499; //Client Closed Request (Nginx)
                    context.Response.ContentType = "text/plain";
                    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes("canceled"));
                    return;
                }
                else if (taskResult.IsFaulted)
                {
                    context.Response.StatusCode = 412; // Precondition Failed (RFC 7232)
                    context.Response.ContentType = "text/plain";
                    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(taskResult.Exception.Message));
                    return;
                }
                else
                {
                    context.Response.StatusCode = 200;

                    if (isJsonRequest)
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(taskResult.Value)));
                    }
                    else if (isDasyncJsonRequest)
                    {
                        context.Response.ContentType = "application/dasync+json";
                        _dasyncJsonSerializer.Serialize(context.Response.Body, taskResult);
                    }
                    else
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(taskResult.Value.ToString()));
                    }

                    return;
                }
            }
            else
            {
                context.Response.StatusCode = 202;

                var location = basePath.ToString() + "/_routine?id=123";
                context.Response.Headers.Add("Location", location);
                context.Response.Headers.Add("ETag", "43627480");
            }
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
}
