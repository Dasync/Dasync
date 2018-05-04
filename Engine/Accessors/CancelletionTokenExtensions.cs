using System.Reflection;
using System.Threading;

namespace Dasync.Accessors
{
    public static class CancelletionTokenExtensions
    {
        public static CancellationTokenSource GetSource(this CancellationToken token)
        {
            var sourceField = typeof(CancellationToken).GetField(
                "m_source", BindingFlags.Instance | BindingFlags.NonPublic);
            return (CancellationTokenSource)sourceField.GetValue(token);
        }
    }
}
