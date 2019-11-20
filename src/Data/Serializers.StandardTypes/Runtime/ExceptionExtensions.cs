using System;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Dasync.Serializers.StandardTypes.Runtime
{
    public static class ExceptionExtensions
    {
        public static void SetClassName(this Exception ex, string className)
        {
            typeof(Exception).GetField("_className", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(ex, className);
        }

        public static void SetMessage(this Exception ex, string message)
        {
            typeof(Exception).GetField("_message", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ex, message);
        }

        public static void SetStackTrace(this Exception ex, string stackTrace)
        {
            typeof(Exception).GetField("_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ex, stackTrace);
        }

        public static void SetInnerException(this Exception ex, Exception innerException)
        {
            typeof(Exception).GetField("_innerException", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ex, innerException);
        }

        public static void SetInnerExceptions(this AggregateException ex, ReadOnlyCollection<Exception> innerExceptions)
        {
            typeof(AggregateException).GetField("m_innerExceptions", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ex, innerExceptions);
        }
    }
}
