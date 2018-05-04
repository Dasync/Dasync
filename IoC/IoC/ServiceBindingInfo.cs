using System;

namespace Dasync.Ioc
{
    public struct ServiceBindingInfo
    {
        public Type ServiceType { get; set; }

        public Type ImplementationType { get; set; }

        public bool IsExternal { get; set; }
    }
}
