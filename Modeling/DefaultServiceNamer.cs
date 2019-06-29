using System;

namespace Dasync.Modeling
{
    public class DefaultServiceNamer
    {
        public static string GetServiceNameFromType(Type type)
        {
            var name = type.Name;

            if (type.IsInterface)
            {
                if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
                    name = name.Substring(1);
            }

            if (name.EndsWith("Service") && name.Length > 7)
                name = name.Substring(0, name.Length - 7);

            return name;
        }
    }
}
