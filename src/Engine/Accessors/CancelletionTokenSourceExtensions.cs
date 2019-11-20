using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Dasync.Accessors
{
    public static class CancelletionTokenSourceExtensions
    {
        public static Timer GetTimer(this CancellationTokenSource source)
        {
            var sourceField = typeof(CancellationTokenSource).GetField(
                "m_timer", BindingFlags.Instance | BindingFlags.NonPublic);
            return (Timer)sourceField.GetValue(source);
        }

        public static void SetTimer(this CancellationTokenSource source, Timer timer)
        {
            var sourceField = typeof(CancellationTokenSource).GetField(
                "m_timer", BindingFlags.Instance | BindingFlags.NonPublic);
            sourceField.SetValue(source, timer);
        }

        public static CancellationTokenRegistration[] GetLinkingRegistrations(
            this CancellationTokenSource source)
        {
            var fieldInfo = typeof(CancellationTokenSource).GetField(
                "m_linkingRegistrations", BindingFlags.Instance | BindingFlags.NonPublic);
            return (CancellationTokenRegistration[])fieldInfo.GetValue(source);
        }

        public static void SetLinkingRegistrations(
            this CancellationTokenSource source,
            CancellationTokenRegistration[] linkedRegistrations)
        {
            var fieldInfo = typeof(CancellationTokenSource).GetField(
                "m_linkingRegistrations", BindingFlags.Instance | BindingFlags.NonPublic);
            fieldInfo.SetValue(source, linkedRegistrations);
        }

        public static TimerCallback TimerCallback
        {
            get
            {
                var fieldInfo = typeof(CancellationTokenSource).GetField(
                    "s_timerCallback", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                return (TimerCallback)fieldInfo.GetValue(null);
            }
        }

        public static CancellationTokenSource[] GetLinkedSources(this CancellationTokenSource source)
        {
            CancellationTokenSource[] result = null;
            var registrations = source.GetLinkingRegistrations();
            source = null;
            if (registrations?.Length > 0)
            {
                result = new CancellationTokenSource[registrations.Length];
                for (var i = 0; i < registrations.Length; i++)
                {
                    var registration = registrations[i];
                    var callbackInfo = registration.GetCallbackInfo();
                    var linkedSource = CancellationCallbackInfoExtenstions
                        .GetCancellationTokenSource(callbackInfo);
                    result[i] = linkedSource;
                }
            }
            return result;
        }

        public static IEnumerable<CancellationTokenSource> EnumerateLinkedSources(
            this CancellationTokenSource source, bool includeNested = false, bool includeItself = false)
        {
            if (includeItself)
                yield return source;

            Queue<CancellationTokenSource> nestedSources = null;

            while (source != null)
            {
                var registrations = source.GetLinkingRegistrations();
                source = null;
                if (registrations?.Length > 0)
                {
                    foreach (var registration in registrations)
                    {
                        var callbackInfo = registration.GetCallbackInfo();
                        var linkedSource = CancellationCallbackInfoExtenstions
                            .GetCancellationTokenSource(callbackInfo);

                        yield return linkedSource;

                        if (includeNested)
                        {
                            if (source == null)
                            {
                                source = linkedSource;
                            }
                            else
                            {
                                if (nestedSources == null)
                                    nestedSources = new Queue<CancellationTokenSource>();
                                nestedSources.Enqueue(linkedSource);
                            }
                        }
                    }
                }

                if (source == null && nestedSources?.Count > 0)
                    source = nestedSources.Dequeue();
            }
        }

        public static DateTime? GetCancellationTime(this CancellationTokenSource source)
            => source.TryGetCancellationTime(out var time) ? (DateTime?)time : null;

        public static bool TryGetCancellationTime(this CancellationTokenSource source, out DateTime cancellationTime)
        {
            var timer = source.GetTimer();
            if (timer != null)
            {
                timer.GetSettings(out var startTime, out var initialDelay, out var interval);
                if (startTime.HasValue && initialDelay.HasValue)
                {
                    cancellationTime = startTime.Value + initialDelay.Value;
                    return true;
                }
            }
            cancellationTime = default(DateTime);
            return false;
        }
    }
}
