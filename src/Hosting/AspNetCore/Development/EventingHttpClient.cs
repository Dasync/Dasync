using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Dasync.Communication.Http;
using Dasync.EETypes.Descriptors;
using Dasync.Serialization;

namespace Dasync.Hosting.AspNetCore.Development
{
    public class EventingHttpClient
    {
        private HttpClient _httpClient;
        private ISerializer _serializer;
        private string _urlBase;
        private bool _compressPayload;
        private MediaTypeHeaderValue _mediaTypeHeaderValue;

        public EventingHttpClient(ISerializer serializer, string urlBase)
        {
            _serializer = serializer;
            _urlBase = urlBase;
            _httpClient = new HttpClient();

            var mediaType = "application/" + _serializer.Format;
            _mediaTypeHeaderValue = new MediaTypeHeaderValue(mediaType);
            if (_serializer is ITextSerializer)
                _mediaTypeHeaderValue.CharSet = "utf-8";
        }

        public async Task SubscribeAsync(EventDescriptor eventDesc, IEnumerable<EventSubscriberDescriptor> subscribers)
        {
            var envelope = new SubscribeEnvelope
            {
                Service = eventDesc.Service,
                Event = eventDesc.Event,
                Subscribers = subscribers.ToList()
            };

            HttpResponseMessage response;
            using (var requestContent = CreateContent(envelope))
            {
                requestContent.Headers.TryAddWithoutValidation(DasyncHttpHeaders.Envelope, "subscribe");
                var url = _urlBase + $"/subscribe?service={eventDesc.Service.Name}&event={eventDesc.Event.Name}";
                response = await _httpClient.PutAsync(url, requestContent);
            }
            using (response)
            {
                if ((int)response.StatusCode == DasyncHttpCodes.Succeeded || (int)response.StatusCode == DasyncHttpCodes.Scheduled)
                    return;

                throw new Exception(); // TODO: add info
            }
        }

        private HttpContent CreateContent(object envelope)
        {
            var bodyStream = new MemoryStream();

            Stream writeStream = bodyStream;
            if (_compressPayload)
                writeStream = new GZipStream(writeStream, CompressionLevel.Optimal, leaveOpen: true);

            _serializer.Serialize(writeStream, envelope);

            if (_compressPayload)
                writeStream.Dispose();

            bodyStream.Position = 0;
            var requestContent = new StreamContent(bodyStream);
            requestContent.Headers.ContentType = _mediaTypeHeaderValue;

            if (_compressPayload)
                requestContent.Headers.ContentEncoding.Add("gzip");

            return requestContent;
        }
    }
}
