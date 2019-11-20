using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Eventing;
using Dasync.Hosting.AspNetCore.Http;
using Dasync.Hosting.AspNetCore.Utils;
using Dasync.Serialization;
using Microsoft.AspNetCore.Http;

namespace Dasync.Hosting.AspNetCore.Development
{
    public class EventingMiddleware : IMiddleware
    {
        private readonly ISerializerProvider _serializerProvider;
        private readonly ISerializer _jsonSerializer;
        private readonly IEventSubscriber _eventSubscriber;
        private PathString _eventingPath;

        public EventingMiddleware(
            ISerializerProvider serializerProvider,
            IEventSubscriber eventSubscriber)
        {
            _serializerProvider = serializerProvider;
            _jsonSerializer = _serializerProvider.GetSerializer("json");
            _eventSubscriber = eventSubscriber;

            _eventingPath = "/dev/events";
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path.StartsWithSegments(_eventingPath))
            {
                await HandleEventingRequest(context);
            }
            else
            {
                await next(context);
            }
        }

        private async Task HandleEventingRequest(HttpContext context)
        {
            var basePathSegmentCount = _eventingPath.Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var pathSegments = context.Request.Path.Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Skip(basePathSegmentCount).ToArray();

            if (pathSegments.Length == 1 && "subscribe".Equals(pathSegments[0], StringComparison.OrdinalIgnoreCase)
                && (context.Request.Method == "PUT") || (context.Request.Method == "POST"))
            {
                await HandleSubscribe(context);
                return;
            }
        }

        private async Task HandleSubscribe(HttpContext context)
        {
            var contentType = context.Request.GetContentType();
            ISerializer serializer;
            try
            {
                serializer = GetSerializer(contentType, false);
            }
            catch (ArgumentException)
            {
                await ReplyWithTextError(context.Response, 406, $"The Content-Type '{contentType.MediaType}' is not supported");
                return;
            }

            var envelope = serializer.Deserialize<SubscribeEnvelope>(context.Request.Body);
            var eventDesc = new EventDescriptor { Service = envelope.Service, Event = envelope.Event };
            foreach (var subscriber in envelope.Subscribers)
                _eventSubscriber.Subscribe(eventDesc, subscriber);

            context.Response.StatusCode = 200;
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
    }
}
