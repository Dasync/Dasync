using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Dasync.DependencyInjection
{
    public class ServiceDescriptorList : List<ServiceDescriptor>, IServiceCollection { }
}
