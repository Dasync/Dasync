using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.EETypes.Configuration
{
    public interface ICommunicationModelConfiguration
    {
        ConfigOverrideLevels GetServiceOverrideLevels(IServiceDefinition serviceDefinition, string sectionName = null);

        IConfiguration GetServiceConfiguration(IServiceDefinition serviceDefinition, string sectionName = null);

        ConfigOverrideLevels GetCommandsOverrideLevels(IServiceDefinition serviceDefinition, string sectionName = null);

        IConfiguration GetCommandsConfiguration(IServiceDefinition serviceDefinition, string sectionName = null);

        ConfigOverrideLevels GetQueriesOverrideLevels(IServiceDefinition serviceDefinition, string sectionName = null);

        IConfiguration GetQueriesConfiguration(IServiceDefinition serviceDefinition, string sectionName = null);

        ConfigOverrideLevels GetEventsOverrideLevels(IServiceDefinition serviceDefinition, string sectionName = null, bool forceExternal = false);

        IConfiguration GetEventsConfiguration(IServiceDefinition serviceDefinition, string sectionName = null, bool forceExternal = false);

        ConfigOverrideLevels GetMethodOverrideLevels(IMethodDefinition methodDefinition, string sectionName = null);

        IConfiguration GetMethodConfiguration(IMethodDefinition methodDefinition, string sectionName = null);

        ConfigOverrideLevels GetEventOverrideLevels(IEventDefinition eventDefinition, string sectionName = null, bool forceExternal = false);

        IConfiguration GetEventConfiguration(IEventDefinition eventDefinition, string sectionName = null, bool forceExternal = false);
    }
}
