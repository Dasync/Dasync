using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.EETypes.Communication;
using Dasync.EETypes.Descriptors;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Communication.Http
{
    public class HttpCommunicator : ICommunicator, ISynchronousCommunicator
    {
        private HttpClient _httpClient;
        private ISerializer _serializer;
        private string _urlTemplate;
        private bool _compressPayload;
        private MediaTypeHeaderValue _mediaTypeHeaderValue;

        public HttpCommunicator(ISerializer serializer, string urlTemplate, bool compressPayload)
        {
            _serializer = serializer;
            _urlTemplate = urlTemplate;
            _httpClient = new HttpClient();
            _compressPayload = compressPayload;

            var mediaType = "application/" + _serializer.Format;
            _mediaTypeHeaderValue = new MediaTypeHeaderValue(mediaType);
            if (_serializer is ITextSerializer)
                _mediaTypeHeaderValue.CharSet = "utf-8";
        }

        public string Type => HttpCommunicationMethod.MethodType;

        public CommunicationTraits Traits =>
            CommunicationTraits.Volatile |
            CommunicationTraits.SyncReplies;

        public async Task<InvokeRoutineResult> InvokeAsync(
            MethodInvocationData data,
            InvocationPreferences preferences)
        {
            HttpResponseMessage response;
            using (var requestContent = CreateContent(data))
            {
                requestContent.Headers.TryAddWithoutValidation(DasyncHttpHeaders.Envelope, "invoke");
                requestContent.Headers.TryAddWithoutValidation(DasyncHttpHeaders.IntentId, data.IntentId);
                AddAsyncHeader(requestContent.Headers, preferAsync: !preferences.Synchronous);
                AddCallerHeaders(requestContent.Headers, data.Caller);
                AddRetryHeader(requestContent.Headers, retryCount: 0);

                var url = _urlTemplate
                    .Replace("{serviceName}", data.Service.Name)
                    .Replace("{methodName}", data.Method.Name);

                response = await _httpClient.PostAsync(url, requestContent);
            }
            using (response)
            {
                if ((int)response.StatusCode == DasyncHttpCodes.Scheduled)
                {
                    return new InvokeRoutineResult
                    {
                        Outcome = InvocationOutcome.Scheduled
                    };
                }

                if ((int)response.StatusCode == DasyncHttpCodes.Deduplicated)
                {
                    return new InvokeRoutineResult
                    {
                        Outcome = InvocationOutcome.Deduplicated
                    };
                }

                var methodOutcome = response.Headers.GetValue(DasyncHttpHeaders.TaskResult);
                if (string.IsNullOrEmpty(methodOutcome))
                {
                    throw new Exception(); // TODO: add info
                }

                var responseStream = await response.Content.ReadAsStreamAsync();

                var encoding = response.Headers.GetContentEncoding();
                if (!string.IsNullOrEmpty(encoding))
                {
                    if ("gzip".Equals(encoding, StringComparison.OrdinalIgnoreCase))
                    {
                        responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    }
                    else if ("deflate".Equals(encoding, StringComparison.OrdinalIgnoreCase))
                    {
                        responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                    }
                    else
                    {
                        throw new Exception($"Unknown content encoding '{encoding}'.");
                    }
                }

                using (responseStream)
                {
                    var taskResult = TaskResult.CreateEmpty(preferences.ResultValueType);
                    _serializer.Populate(responseStream, (IValueContainer)taskResult);

                    return new InvokeRoutineResult
                    {
                        Outcome = InvocationOutcome.Complete,
                        Result = taskResult
                    };
                }
            }
        }

        public async Task<ContinueRoutineResult> ContinueAsync(
            MethodContinuationData data,
            InvocationPreferences preferences)
        {
            HttpResponseMessage response;
            using (var requestContent = CreateContent(data))
            {
                requestContent.Headers.TryAddWithoutValidation(DasyncHttpHeaders.Envelope, "continue");
                requestContent.Headers.TryAddWithoutValidation(DasyncHttpHeaders.IntentId, data.IntentId);
                AddAsyncHeader(requestContent.Headers, preferAsync: true);
                AddCallerHeaders(requestContent.Headers, data.Caller);
                AddRetryHeader(requestContent.Headers, retryCount: 0);

                var url = _urlTemplate
                    .Replace("{serviceName}", data.Service.Name)
                    .Replace("{methodName}", data.Method.Name)
                    + "/" + data.Method.IntentId;

                if (!string.IsNullOrEmpty(data.Method.ETag))
                    url += "?etag=" + data.Method.ETag;

                response = await _httpClient.PostAsync(url, requestContent);
            }
            using (response)
            {
                if ((int)response.StatusCode == DasyncHttpCodes.Scheduled)
                {
                    return new ContinueRoutineResult
                    {
                    };
                }

                if ((int)response.StatusCode == DasyncHttpCodes.Deduplicated)
                {
                    return new ContinueRoutineResult
                    {
                    };
                }
                throw new Exception(); // TODO: add info
            }
        }

        public async Task<InvokeRoutineResult> GetInvocationResultAsync(
            ServiceId serviceId,
            MethodId methodId,
            string intentId,
            Type resultValueType,
            CancellationToken ct)
        {
            var url = _urlTemplate
                .Replace("{serviceName}", serviceId.Name)
                .Replace("{methodName}", methodId.Name)
                + "/" + intentId;

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };

            request.Headers.TryAddWithoutValidation(DasyncHttpHeaders.Envelope, "poll");

            if (_compressPayload)
                request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip");

            // TODO: add time for long polling
            //AddAsyncHeader(message.Headers, preferAsync: false, waitTime: );

            HttpResponseMessage response;
            using (request)
            {
                response = await _httpClient.SendAsync(request, ct);
            }

            using (response)
            {
                if ((int)response.StatusCode == DasyncHttpCodes.Running)
                {
                    return new InvokeRoutineResult
                    {
                        Outcome = InvocationOutcome.Unknown
                    };
                }

                var methodOutcome = response.Headers.GetValue(DasyncHttpHeaders.TaskResult);
                if (string.IsNullOrEmpty(methodOutcome))
                {
                    throw new Exception(); // TODO: add info
                }

                var responseStream = await response.Content.ReadAsStreamAsync();

                var encoding = response.Headers.GetContentEncoding();
                if (!string.IsNullOrEmpty(encoding))
                {
                    if ("gzip".Equals(encoding, StringComparison.OrdinalIgnoreCase))
                    {
                        responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    }
                    else if ("deflate".Equals(encoding, StringComparison.OrdinalIgnoreCase))
                    {
                        responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                    }
                    else
                    {
                        throw new Exception($"Unknown content encoding '{encoding}'.");
                    }
                }

                using (responseStream)
                {
                    var taskResult = TaskResult.CreateEmpty(resultValueType);
                    _serializer.Populate(responseStream, (IValueContainer)taskResult);

                    return new InvokeRoutineResult
                    {
                        Outcome = InvocationOutcome.Complete,
                        Result = taskResult
                    };
                }
            }
        }

        private void AddCallerHeaders(HttpContentHeaders headers, CallerDescriptor caller)
        {
            if (caller == null)
                return;

            if (!string.IsNullOrEmpty(caller.IntentId))
                headers.TryAddWithoutValidation(DasyncHttpHeaders.CallerIntentId, caller.IntentId);

            if (caller.Service != null)
            {
                headers.TryAddWithoutValidation(DasyncHttpHeaders.CallerServiceName, caller.Service.Name);
                if (!string.IsNullOrEmpty(caller.Service.Proxy))
                    headers.TryAddWithoutValidation(DasyncHttpHeaders.CallerServiceProxy, caller.Service.Proxy);
            }

            if (caller.Method != null)
                headers.TryAddWithoutValidation(DasyncHttpHeaders.CallerMethodName, caller.Method.Name);

            if (caller.Event != null)
                headers.TryAddWithoutValidation(DasyncHttpHeaders.CallerEventName, caller.Event.Name);
        }

        private void AddRetryHeader(HttpContentHeaders headers, int retryCount)
        {
            headers.TryAddWithoutValidation(DasyncHttpHeaders.Retry, retryCount > 0 ? "true" : "false");
        }

        private void AddAsyncHeader(HttpContentHeaders headers, bool preferAsync, TimeSpan? waitTime = null)
        {
            if (!preferAsync && !waitTime.HasValue)
                return;

            var headerValue = preferAsync ? "respond-async" : null;

            if (waitTime.HasValue)
            {
                if (headerValue != null)
                    headerValue += ", ";
                headerValue += "wait=";
                headerValue += (int)waitTime.Value.TotalSeconds;
            }

            headers.TryAddWithoutValidation("Prefer", headerValue);
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
