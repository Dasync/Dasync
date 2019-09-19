using System;
using System.Collections.Generic;

namespace Dasync.AspNetCore
{
    public class HttpStatusCodeExceptionMap
    {
        private readonly Dictionary<Type, int> _mapping = new Dictionary<Type, int>();

        public bool AddMapping(Type exceptionType, int statusCode)
        {
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
                return false;

            if (_mapping.ContainsKey(exceptionType))
                return false;

            _mapping.Add(exceptionType, statusCode);
            return true;
        }

        public bool TryMap(Type exceptionType, out int statusCode)
        {
            if (_mapping.TryGetValue(exceptionType, out statusCode))
                return true;

            foreach (var pair in _mapping)
            {
                if (pair.Key.IsAssignableFrom(exceptionType))
                {
                    statusCode = pair.Value;
                    return true;
                }
            }

            statusCode = -1;
            return false;
        }
    }
}
