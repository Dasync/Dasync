using System;

namespace Dasync.EETypes.Configuration
{
    [Flags]
    public enum ConfigOverrideLevels
    {
        None = 0,

        /// <summary>
        /// Path: "dasync"
        /// </summary>
        Base = 1,

        /// <summary>
        /// Path: "dasync:queries"
        /// Path: "dasync:commands"
        /// Path: "dasync:events"
        /// </summary>
        BasePrimitives = 2,

        /// <summary>
        /// Path: "dasync:services:_local"
        /// Path: "dasync:services:_external"
        /// </summary>
        ServiceType = 4,

        /// <summary>
        /// Path: "dasync:services:_local:queries"
        /// Path: "dasync:services:_local:commands"
        /// Path: "dasync:services:_local:events"
        /// Path: "dasync:services:_external:queries"
        /// Path: "dasync:services:_external:commands"
        /// Path: "dasync:services:_external:events"
        /// </summary>
        ServiceTypePrimitives = 8,

        /// <summary>
        /// Path: "dasync:services:{service-name}"
        /// Path: "dasync:services:{service-name}"
        /// Path: "dasync:services:{service-name}"
        /// </summary>
        Service = 16,

        /// <summary>
        /// Path: "dasync:services:{service-name}:queries"
        /// Path: "dasync:services:{service-name}:commands"
        /// Path: "dasync:services:{service-name}:events"
        /// </summary>
        ServicePrimitives = 32,

        /// <summary>
        /// Path: "dasync:services:{service-name}:queries:{command-name}"
        /// Path: "dasync:services:{service-name}:commands:{command-name}"
        /// Path: "dasync:services:{service-name}:events:{command-name}"
        /// </summary>
        Primitive = 64
    }
}
