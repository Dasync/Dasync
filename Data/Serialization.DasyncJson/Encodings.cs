using System.Text;

namespace Dasync.Serialization.DasyncJson
{
    internal static class Encodings
    {
        public static readonly Encoding UTF8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
