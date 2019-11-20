using System;

namespace Dasync.Hosting.AspNetCore.Http
{
    public struct RFC7240Preferences
    {
        public bool? RespondAsync { get; set; }

        public TimeSpan? Wait { get; set; }
    }
}
