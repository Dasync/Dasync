using System.Collections.Specialized;
using System.Linq;

namespace Dasync.AspNetCore.Platform
{
    public static class NameValueCollectionExtensions
    {
        public static string Serialize(this NameValueCollection c) =>
            c?.Count > 0
            ? string.Join("; ", c.AllKeys.Select(key => string.Concat(key, "=", string.Join(",", c.GetValues(key)))))
            : string.Empty;

        public static NameValueCollection Deserialize(string str) =>
            new NameValueCollection().Load(str);

        public static NameValueCollection Load(this NameValueCollection c, string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return c;

            foreach (var pair in str.Split(';'))
            {
                var equalsIndex = pair.IndexOf('=');
                if (equalsIndex <= 0)
                    continue;

                var key = pair.Substring(0, equalsIndex).Trim();
                var valuesStr = pair.Substring(equalsIndex + 1);

                if (valuesStr.Contains(','))
                {
                    foreach (var value in valuesStr.Split(','))
                    {
                        c.Add(key, value.Trim());
                    }
                }
                else
                {
                    c.Add(key, valuesStr.Trim());
                }
            }

            return c;
        }
    }
}
