using System;
using System.Text;

namespace Dasync.Serialization
{
    public sealed class AssemblySerializationInfo
    {
        private static readonly Version EmptyVersion = new Version(0, 0, 0, 0);

        public string Name;
        public Version Version;
        public string Token;
        public string Culture;

        public override bool Equals(object obj)
        {
            if (obj is AssemblySerializationInfo info)
                return this == info;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashcode = 0;
                if (Name != null)
                    hashcode = Name.GetHashCode();
                if (Version != null && (Version.Major != 0 || Version.Minor != 0 || Version.Build != 0 || Version.Revision != 0))
                    hashcode = hashcode * 104651 + Version.GetHashCode();
                if (Token != null && Token.Length > 0)
                    hashcode = hashcode * 104651 + Token.GetHashCode();
                return hashcode;
            }
        }

        public static bool operator ==(AssemblySerializationInfo info1, AssemblySerializationInfo info2)
        {
            if (info1 is null && info2 is null)
                return true;

            if (info1 is null || info2 is null)
                return false;

            return info1.Name == info2.Name
                && (info1.Version ?? EmptyVersion) == (info2.Version ?? EmptyVersion)
                && (info1.Token ?? string.Empty) == (info2.Token ?? string.Empty);
        }

        public static bool operator !=(AssemblySerializationInfo info1, AssemblySerializationInfo info2)
            => !(info1 == info2);

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToStringInternal(sb);
            return sb.ToString();
        }

        internal void ToStringInternal(StringBuilder sb)
        {
            sb.Append(Name);
            if (Version != null && (Version.Major != 0 || Version.Minor != 0 || Version.Build != 0 || Version.Revision != 0))
                sb.Append(", Version=").Append(Version.ToString());
            if (Culture != null)
                sb.Append(", Culture=").Append(Culture);
            if (Token != null && Token.Length > 0)
                sb.Append(", PublicKeyToken=").Append(Token);
        }

        public static AssemblySerializationInfo Parse(string fullName)
        {
            return ParseInternal(fullName, 0, fullName.Length);
        }

        internal static AssemblySerializationInfo ParseInternal(string fullName, int startIndex, int endIndex)
        {
            var result = new AssemblySerializationInfo();

            // Skip whitespace
            while (startIndex < endIndex && fullName[startIndex] == ' ') startIndex++;

            // Parse Name
            var nameEndIndex = startIndex;
            for (; nameEndIndex < endIndex; nameEndIndex++)
            {
                if (fullName[nameEndIndex] == ',' ||
                    fullName[nameEndIndex] == ' ')
                    break;
            }
            result.Name = fullName.Substring(startIndex, nameEndIndex - startIndex);
            startIndex = nameEndIndex;

            // Skip whitespace
            while (startIndex < endIndex && fullName[startIndex] == ' ') startIndex++;

            // Pairs
            while (startIndex < endIndex && fullName[startIndex] == ',')
            {
                startIndex++;

                // Skip whitespace
                while (startIndex < endIndex && fullName[startIndex] == ' ') startIndex++;

                var pairEndIndex = fullName.IndexOf(',', startIndex);
                if (pairEndIndex < 0 || pairEndIndex > endIndex)
                    pairEndIndex = endIndex;

                // Parse Key
                var keyEndIndex = startIndex;
                for (; keyEndIndex < pairEndIndex; keyEndIndex++)
                {
                    if (fullName[keyEndIndex] == '=' ||
                        fullName[keyEndIndex] == ' ')
                        break;
                }
                var key = fullName.Substring(startIndex, keyEndIndex - startIndex);
                startIndex = keyEndIndex;

                // Skip whitespace
                while (startIndex < endIndex && fullName[startIndex] == ' ') startIndex++;

                if (fullName[startIndex] != '=')
                    throw new InvalidOperationException("Assembly full name format error");
                startIndex++;

                // Skip whitespace
                while (startIndex < endIndex && fullName[startIndex] == ' ') startIndex++;

                // Parse Value
                var valueEndIndex = startIndex;
                for (; valueEndIndex < pairEndIndex; valueEndIndex++)
                {
                    if (fullName[valueEndIndex] == ',' ||
                        fullName[valueEndIndex] == ' ')
                        break;
                }
                var value = fullName.Substring(startIndex, valueEndIndex - startIndex);
                startIndex = valueEndIndex;

                switch (key)
                {
                    case "Version":
                        result.Version = Version.Parse(value);
                        break;
                    case "Culture":
                        result.Culture = value;
                        break;
                    case "PublicKeyToken":
                        result.Token = value;
                        break;
                    default:
                        throw new InvalidOperationException("Assembly full name unknown property " + key);
                }

                // Skip whitespace
                while (startIndex < endIndex && fullName[startIndex] == ' ') startIndex++;
            }

            return result;
        }
    }
}
