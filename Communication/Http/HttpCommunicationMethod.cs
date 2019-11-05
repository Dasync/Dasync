using Dasync.EETypes.Communication;
using Dasync.Serialization;
using Microsoft.Extensions.Configuration;

namespace Dasync.Communication.Http
{
    public class HttpCommunicationMethod : ICommunicationMethod
    {
        public const string MethodType = "HTTP";

        private readonly ISerializer _defaultSerializer;
        private readonly ISerializerProvider _serializerProvider;

        public HttpCommunicationMethod(
            IDefaultSerializerProvider defaultSerializerProvider,
            ISerializerProvider serializerProvider)
        {
            _defaultSerializer = defaultSerializerProvider.DefaultSerializer;
            _serializerProvider = serializerProvider;
        }

        public string Type => MethodType;

        public ICommunicator CreateCommunicator(IConfiguration configuration)
        {
            var settings = new HttpCommunicatorSettings();
            configuration.Bind(settings);

            var serializer = string.IsNullOrEmpty(settings.Serializer)
                ? _defaultSerializer
                : _serializerProvider.GetSerializer(settings.Serializer);
            var compressPayload = settings.Compress ?? false;
            var urlTemplate = GetUrlTemplate(settings);

            return new HttpCommunicator(serializer, urlTemplate, compressPayload);
        }

        private string GetUrlTemplate(HttpCommunicatorSettings settings)
        {
            var url = settings.Url;
            if (string.IsNullOrEmpty(url))
            {
                var address = settings.Address;
                if (string.IsNullOrEmpty(address))
                {
                    var scheme = settings.Https == true ? "https" : "http";
                    var host = !string.IsNullOrEmpty(settings.Host) ? settings.Host : "{serviceName}";
                    address = scheme + "://" + host;
                    if (settings.Port != null)
                        address += ":" + settings.Port;
                }

                if (address.EndsWith("/"))
                    address = address.Substring(0, address.Length - 1);

                var path = settings.Path;
                if (string.IsNullOrEmpty(path))
                {
                    var apiSegment = !string.IsNullOrEmpty(settings.ApiSegment) ? settings.ApiSegment : "/api";
                    if (!apiSegment.StartsWith("/"))
                        apiSegment = "/" + apiSegment;

                    var methodPath = !string.IsNullOrEmpty(settings.MethodPath) ? settings.MethodPath : "/{serviceName}/{methodName}";
                    if (!methodPath.StartsWith("/"))
                        methodPath = "/" + methodPath;

                    path = apiSegment + methodPath;
                }

                if (!path.StartsWith("/"))
                    path = "/" + path;

                url = address + path;
            }
            return url;
        }
    }
}
