using System;
using System.Linq;
using Dasync.Communication.Http;
using Microsoft.AspNetCore.Http;

namespace Dasync.Hosting.AspNetCore.Http
{
    public static class HttpHeadersExtensions
    {
        public static string GetValue(this IHeaderDictionary headers, string headerName)
        {
            return headers.TryGetValue(headerName, out var values) && values.Count > 0 ? values[0] : null;
        }

        public static bool ContainsValue(this IHeaderDictionary headers, string headerName, string value)
        {
            return headers.TryGetValue(headerName, out var values)
                && values.Count > 0
                && values.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        public static bool? IsRetry(this IHeaderDictionary headers)
        {
            if (!headers.TryGetValue(DasyncHttpHeaders.Retry, out var retryValues))
                return null;

            if (retryValues.Count == 0)
                return true;

            var value = retryValues[0];

            if ("true".Equals(value, StringComparison.OrdinalIgnoreCase))
                return true;

            if ("false".Equals(value, StringComparison.OrdinalIgnoreCase))
                return false;

            if (int.TryParse(value, out var retryCount))
                return retryCount > 0;

            return null;
        }

        public static RFC7240Preferences GetRFC7240Preferences(this IHeaderDictionary headers)
        {
            // EXAMPLE
            // Prefer: respond-async, wait=10

            var preferences = new RFC7240Preferences();

            if (headers.TryGetValue("Prefer", out var values))
            {
                for (var i = 0; i < values.Count; i++)
                {
                    var headerValue = values[i];

                    // Yes, if somebody types 'Foo#respond-async#Bar' the condition succeeds as well, but c'mon!
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
    }
}
