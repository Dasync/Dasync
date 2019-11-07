using System.Net.Mime;
using Microsoft.AspNetCore.Http;

namespace Dasync.Hosting.AspNetCore.Http
{
    public static class HttpRequestContentExtensions
    {
        private static readonly ContentType UnknownContentType = new ContentType();

        public static ContentType GetContentType(this HttpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ContentType))
                return UnknownContentType;
            return new ContentType(request.ContentType);
        }

        public static string GetContentEncoding(this HttpRequest request)
        {
            if (!request.Headers.TryGetValue("Content-Encoding", out var values) || values.Count == 0)
                return null;
            return values[0];
        }
    }
}
