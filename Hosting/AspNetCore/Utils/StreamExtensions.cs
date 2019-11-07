using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dasync.Hosting.AspNetCore.Utils
{
    public static class StreamExtensions
    {
        private static Encoding UTF8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public static Task WriteUtf8StringAsync(this Stream stream, string text, CancellationToken ct = default)
        {
            var bytes = UTF8.GetBytes(text);
            return stream.WriteAsync(bytes, 0, bytes.Length, ct);
        }

        public static async Task<byte[]> ToBytesAsync(this Stream stream, CancellationToken ct = default)
        {
            var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, 1024, ct);
            return buffer.ToArray();
        }
    }
}
