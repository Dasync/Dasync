using System;

namespace Dasync.AzureStorage
{
    public class ConnectionStringParser
    {
        public static string GetAccountName(string connectionString)
        {
            return GetValue(connectionString, "AccountName");
        }

        public static string GetValue(string connectionString, string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (string.IsNullOrEmpty(connectionString))
                return null;

            var keyLength = key.Length;
            var keyIndex = connectionString.IndexOf(key);
#warning need to check previous symbol - must be ';' where preceeding whitespace is permittable?
#warning need to check the symbol after the key - must be '=' where preceeding whitespace is permittable?
            if (keyIndex < 0)
                throw new ArgumentException(
                    $"Could not find '{key}' in the connection string.",
                    nameof(connectionString));

            var valueStartIndex = keyIndex + keyLength + 1;

            var colonIndex = connectionString.IndexOf(';', valueStartIndex);
            if (colonIndex < 0)
                colonIndex = connectionString.Length;

            var valueLength = colonIndex - valueStartIndex;
            if (valueLength == 0)
                return string.Empty;

            return connectionString.Substring(valueStartIndex, valueLength);
        }
    }
}
