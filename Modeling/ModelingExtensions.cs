using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Dasync.Modeling
{
    public static class ModelingExtensions
    {
        public static bool IsRoutineCandidate(this MethodInfo methodInfo)
        {
            if (!methodInfo.IsFamily && !methodInfo.IsPublic)
                return false;

            if (!methodInfo.IsVirtual)
                return false;

            if (methodInfo.IsAbstract && !methodInfo.DeclaringType.IsInterface)
                return false;

            if (methodInfo.ContainsGenericParameters)
                return false;

            if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                // An exception from the rule for service instances.
                if (methodInfo.ReturnType == typeof(void) &&
                    methodInfo.Name == nameof(IDisposable.Dispose) &&
                    methodInfo.GetParameters().Length == 0)
                    return true;

                return false;
            }

            return true;
        }

        public static bool HasQueryImplyingName(this MethodInfo methodInfo) =>
            IsQueryImplyingName(methodInfo.Name);

        public static bool IsQueryImplyingName(string methodName) =>
            GetWordAndSynonyms.Contains(GetFirstWord(methodName));

        private static string GetFirstWord(string text)
        {
            if (text.Length <= 1)
                return text;

            for (var i = 1; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsUpper(c) || c == '_' || c == '-' || c == '+' || c == '#' || c == '@' || c == '!' || char.IsWhiteSpace(c))
                {
                    return text.Substring(0, i);
                }
            }

            return text;
        }

        private static readonly HashSet<string> GetWordAndSynonyms =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Get",
                "Query",
                "Find",
                "Search",
                "List",
                "Fetch",
                "Retrieve",
                "Collect",
                "Select",
                "Take"
            };
    }
}
