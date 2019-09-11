using Microsoft.AspNetCore.Http;

namespace Dasync.Azure.Functions.AspNetCoreMiddleware
{
    internal class RequestDelegateHolder
    {
        public RequestDelegate RequestDelegate { get; set; }
    }
}
