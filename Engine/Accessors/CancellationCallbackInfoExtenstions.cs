using System.Reflection;
using System.Threading;

namespace Dasync.Accessors
{
    public static class CancellationCallbackInfoExtenstions
    {
        public static CancellationTokenSource GetCancellationTokenSource(object callbackInfo)
        {
            var sourceField = callbackInfo.GetType().GetField(
                "CancellationTokenSource", BindingFlags.Instance | BindingFlags.NonPublic);
            return (CancellationTokenSource)sourceField.GetValue(callbackInfo);
        }
    }
}
