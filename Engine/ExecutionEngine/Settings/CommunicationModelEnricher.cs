using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Modeling;
using Microsoft.Extensions.Configuration;

namespace Dasync.ExecutionEngine.Communication
{
    public class CommunicationModelEnricher : ICommunicationModelEnricher
    {
        private readonly IConfiguration _config;

        public CommunicationModelEnricher(IEnumerable<IConfiguration> safeConfiguration)
        {
            _config = safeConfiguration.FirstOrDefault()?.GetSection("dasync");
        }

        public void Enrich(IMutableCommunicationModel model, bool rootOnly = false)
        {
            if (_config == null)
                return;

            var localServiceSection = _config.GetSection("services:_local");
            var externalServiceSection = _config.GetSection("services:_external");

            ReadCommunicationType(_config, model, string.Empty);
            ReadCommunicationType(localServiceSection, model, ":local");
            ReadCommunicationType(externalServiceSection, model, ":external");

            ReadPersistenceType(_config, model, string.Empty);
            ReadPersistenceType(localServiceSection, model, ":local");
            ReadPersistenceType(externalServiceSection, model, ":external");

            ReadBehaviorOptions(_config, model, string.Empty);
            ReadBehaviorOptions(localServiceSection, model, ":local");
            ReadBehaviorOptions(externalServiceSection, model, ":external");

            if (rootOnly)
                return;

            var configuredServiceNames =
                _config
                .GetSection("services")
                .GetChildren()
                .Select(c => c.Key)
                .Where(n => n != "_local" && n != "_external");

            foreach (var serviceName in configuredServiceNames)
            {
                var service = model.FindServiceByName(serviceName) as IMutableServiceDefinition;
                if (service == null)
                    continue;
                var serviceSection = _config.GetSection("services:" + serviceName);
                Enrich(service, serviceSection);
            }
        }

        public void Enrich(IMutableServiceDefinition service, bool serviceOnly = false)
        {
            var serviceSection = _config.GetSection("services:" + service.Name);
            Enrich(service, serviceSection, serviceOnly);
        }

        private void Enrich(IMutableServiceDefinition service, IConfigurationSection serviceSection, bool serviceOnly = false)
        {
            ReadCommunicationType(serviceSection, service, string.Empty, ":_all");
            ReadPersistenceType(serviceSection, service, string.Empty, ":_all");
            ReadBehaviorOptions(serviceSection, service, string.Empty, ":_all");

            if (serviceOnly)
                return;

            var configuredQueryNames =
                serviceSection
                .GetSection("queries")
                .GetChildren()
                .Select(c => c.Key)
                .Where(n => n != "_all");

            foreach (var queryName in configuredQueryNames)
            {
                var method = service.FindMethod(queryName) as IMutableMethodDefinition;
                if (method == null || method.IsIgnored || !method.IsQuery) // TODO: use config as an override for the method type?
                    continue;

                var methodSection = serviceSection.GetSection("queries:" + queryName);

                Enrich(method, methodSection);
            }

            var configuredCommandNames =
                serviceSection
                .GetSection("commands")
                .GetChildren()
                .Select(c => c.Key)
                .Where(n => n != "_all");

            foreach (var commandName in configuredCommandNames)
            {
                var method = service.FindMethod(commandName) as IMutableMethodDefinition;
                if (method == null || method.IsIgnored || method.IsQuery) // TODO: use config as an override for the method type?
                    continue;

                var methodSection = serviceSection.GetSection("commands:" + commandName);

                Enrich(method, methodSection);
            }

            var configuredEventNames =
                serviceSection
                .GetSection("events")
                .GetChildren()
                .Select(c => c.Key)
                .Where(n => n != "_all");

            foreach (var eventName in configuredEventNames)
            {
                var @event = service.FindEvent(eventName) as IMutableEventDefinition;
                if (@event == null)
                    continue;

                var eventSection = serviceSection.GetSection("events:" + eventName);

                var eventCommunicationType = eventSection.GetSection("communication:type").Value;
                if (!string.IsNullOrWhiteSpace(eventCommunicationType))
                    @event.AddProperty("communicationType", eventCommunicationType);

                ReadEventBahavior(eventSection, @event, string.Empty);
            }
        }

        public void Enrich(IMutableMethodDefinition method)
        {
            var serviceSection = _config.GetSection("services:" + method.Service.Name);
            var methodSection = serviceSection.GetSection("commands:" + method.Name);
            Enrich(method, methodSection);
        }

        private void Enrich(IMutableMethodDefinition method, IConfigurationSection methodSection)
        {
            var methodCommunicationType = methodSection.GetSection("communication:type").Value;
            if (!string.IsNullOrWhiteSpace(methodCommunicationType))
                method.AddProperty("communicationType", methodCommunicationType);

            ReadMethodBahavior(methodSection, method, string.Empty);
        }

        private static void ReadCommunicationType(IConfiguration config, IMutablePropertyBag propertyBag, string suffix, string selector = "")
        {
            var communicationType = config.GetSection("communication:type").Value;
            if (!string.IsNullOrWhiteSpace(communicationType))
                propertyBag.AddProperty("communicationType" + suffix, communicationType);

            var defaultQueriesCommunicationType = config.GetSection("queries" + selector + ":communication:type").Value;
            if (!string.IsNullOrWhiteSpace(defaultQueriesCommunicationType))
                propertyBag.AddProperty("communicationType:queries" + suffix, defaultQueriesCommunicationType);

            var defaultCommandsCommunicationType = config.GetSection("commands" + selector + ":communication:type").Value;
            if (!string.IsNullOrWhiteSpace(defaultCommandsCommunicationType))
                propertyBag.AddProperty("communicationType:commands" + suffix, defaultCommandsCommunicationType);

            var defaultEventsCommunicationType = config.GetSection("events" + selector + ":communication:type").Value;
            if (!string.IsNullOrWhiteSpace(defaultEventsCommunicationType))
                propertyBag.AddProperty("communicationType:events" + suffix, defaultEventsCommunicationType);
        }

        private static void ReadPersistenceType(IConfiguration config, IMutablePropertyBag propertyBag, string suffix, string selector = "")
        {
            var persistenceType = config.GetSection("persistence:type").Value;
            if (!string.IsNullOrWhiteSpace(persistenceType))
                propertyBag.AddProperty("persistenceType" + suffix, persistenceType);

            var defaultQueriesPersistenceType = config.GetSection("queries" + selector + ":persistence:type").Value;
            if (!string.IsNullOrWhiteSpace(defaultQueriesPersistenceType))
                propertyBag.AddProperty("persistenceType:queries" + suffix, defaultQueriesPersistenceType);

            var defaultCommandsPersistenceType = config.GetSection("commands" + selector + ":persistence:type").Value;
            if (!string.IsNullOrWhiteSpace(defaultCommandsPersistenceType))
                propertyBag.AddProperty("persistenceType:commands" + suffix, defaultCommandsPersistenceType);

            var defaultEventsPersistenceType = config.GetSection("events" + selector + ":persistence:type").Value;
            if (!string.IsNullOrWhiteSpace(defaultEventsPersistenceType))
                propertyBag.AddProperty("persistenceType:events" + suffix, defaultEventsPersistenceType);
        }

        private static void ReadBehaviorOptions(IConfiguration config, IMutablePropertyBag propertyBag, string suffix, string selector = "")
        {
            ReadMethodBahavior(config.GetSection("queries" + selector), propertyBag, ":queries" + suffix);
            ReadMethodBahavior(config.GetSection("commands" + selector), propertyBag, ":commands" + suffix);
            ReadEventBahavior(config.GetSection("events" + selector), propertyBag, ":events" + suffix);
        }

        private static void ReadMethodBahavior(IConfiguration config, IMutablePropertyBag propertyBag, string suffix)
        {
            var behaviorOptions = new MethodBehaviorOptions();
            config.Bind(behaviorOptions);
            if (behaviorOptions.Deduplicate.HasValue)
                propertyBag.AddProperty("deduplicate" + suffix, behaviorOptions.Deduplicate.Value);
            if (behaviorOptions.Resilient.HasValue)
                propertyBag.AddProperty("resilient" + suffix, behaviorOptions.Resilient.Value);
            if (behaviorOptions.Persistent.HasValue)
                propertyBag.AddProperty("persistent" + suffix, behaviorOptions.Persistent.Value);
            if (behaviorOptions.RoamingState.HasValue)
                propertyBag.AddProperty("roamingstate" + suffix, behaviorOptions.RoamingState.Value);
            if (behaviorOptions.Transactional.HasValue)
                propertyBag.AddProperty("transactional" + suffix, behaviorOptions.Transactional.Value);
            if (behaviorOptions.RunInPlace.HasValue)
                propertyBag.AddProperty("runinplace" + suffix, behaviorOptions.RunInPlace.Value);
            if (behaviorOptions.IgnoreTransaction.HasValue)
                propertyBag.AddProperty("ignoretransaction" + suffix, behaviorOptions.IgnoreTransaction.Value);
        }

        private static void ReadEventBahavior(IConfiguration config, IMutablePropertyBag propertyBag, string suffix)
        {
            var behaviorOptions = new EventBehaviorOptions();
            config.Bind(behaviorOptions);
            if (behaviorOptions.Deduplicate.HasValue)
                propertyBag.AddProperty("deduplicate" + suffix, behaviorOptions.Deduplicate.Value);
            if (behaviorOptions.Resilient.HasValue)
                propertyBag.AddProperty("resilient" + suffix, behaviorOptions.Resilient.Value);
            if (behaviorOptions.IgnoreTransaction.HasValue)
                propertyBag.AddProperty("ignoretransaction" + suffix, behaviorOptions.IgnoreTransaction.Value);
        }

        public class MethodBehaviorOptions
        {
            /// <summary>
            /// Deduplicate messages on receive when the receiving communication method supports
            /// such a feature, or if a UoW/cache mechanism is available in the app.
            /// </summary>
            public bool? Deduplicate { get; set; }

            /// <summary>
            /// Prefer a method to be executed via a communication method that has a message delivery guarantee.
            /// For example, an HTTP invocation can delegate execution to a message queue.
            /// Resiliency cannot be guaranteed if the desired communication method does not support it.
            /// </summary>
            public bool? Resilient { get; set; }

            /// <summary>
            /// Method state should be saved and restored when sending commands (sometimes queries).
            /// If FALSE, then wait for the command completion in process.
            /// Persistence won't be available if there are no meachnisms registered in an app,
            /// unless <see cref="RoamingState"/> is set to TRUE.
            /// </summary>
            public bool? Persistent { get; set; }

            /// <summary>
            /// When <see cref="Persistent"/> is enabled, convey the state of a method inside a command,
            /// so the state is restored from the response instead of a persistence mechanism.
            /// Does not work with <see cref="Task.WhenAll"/> because it requires concurrency check.
            /// Has no effect if a command (sometimes queries) has no continuation.
            /// </summary>
            public bool? RoamingState { get; set; }

            /// <summary>
            /// Enables Unit of Work - send all commands/events and save DB entities at once
            /// when a method transition completes. DB integration is not guaranteed.
            /// When FALSE, any command or event (sometimes queries) will be sent immediately
            /// without any wait for the transition to complete. There are multiple implementation
            /// options like outbox pattern or caching and message completion table.
            /// </summary>
            public bool? Transactional { get; set; }

            /// <summary>
            /// Prefer to run a query or a command in the same process instead of scheduling a message.
            /// Queries of local services are invoked in place by default. If a command has the Persistent
            /// option, then invocation in place is only possible when the communication method supports a
            /// message lock. When message is created, you can think of this behavior as 'high priority'
            /// (cut in front of other messages on the queue) and 'low latency' (no need to wait till the
            /// message is picked up, processed, and the result is polled back.)
            /// </summary>
            public bool? RunInPlace { get; set; }

            /// <summary>
            /// Ignore the transaction even if the method that calls this command (sometimes a query)
            /// is transctional. This allows better latency but no consistency.
            /// Queries ignore transactions by default.
            /// </summary>
            public bool? IgnoreTransaction { get; set; }
        }

        public class EventBehaviorOptions
        {
            /// <summary>
            /// Deduplicate messages on receive when the receiving communication method supports
            /// such a feature, or if a UoW/cache mechanism is available in the app.
            /// </summary>
            public bool? Deduplicate { get; set; }

            /// <summary>
            /// Prefer an event to be executed via a communication method that has a message delivery guarantee.
            /// For example, an HTTP invocation can delegate execution to an event stream.
            /// Resiliency cannot be guaranteed if the desired communication method does not support it.
            /// </summary>
            public bool? Resilient { get; set; }

            /// <summary>
            /// Ignore the transaction even if the method that publishes this event is transctional.
            /// This allows better latency but no consistency.
            /// </summary>
            public bool? IgnoreTransaction { get; set; }
        }
    }
}
