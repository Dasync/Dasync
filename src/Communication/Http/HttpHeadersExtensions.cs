using System;
using System.Linq;
using System.Net.Http.Headers;

namespace Dasync.Communication.Http
{
    public static class HttpHeadersExtensions
    {
        public static string GetValue(this HttpResponseHeaders headers, string headerName)
        {
            return headers.TryGetValues(headerName, out var values) ? values.FirstOrDefault() : null;
        }

        public static bool ContainsValue(this HttpResponseHeaders headers, string headerName, string value)
        {
            return headers.TryGetValues(headerName, out var values)
                && values.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        public static string GetContentEncoding(this HttpResponseHeaders headers) => headers.GetValue("Content-Encoding");
    }
}
