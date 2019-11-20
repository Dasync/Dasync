using System.Reflection;
using System.Threading;

namespace Dasync.Accessors
{
    public static class CancellationTokenRegistrationExtensions
    {
        public static object GetCallbackInfo(this CancellationTokenRegistration registration)
        {
            var sourceField = typeof(CancellationTokenRegistration).GetField(
                "m_callbackInfo", BindingFlags.Instance | BindingFlags.NonPublic);
            return sourceField.GetValue(registration);
        }
    }
}
