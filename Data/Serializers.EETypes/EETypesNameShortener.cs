using System;
using System.Collections.Generic;
using Dasync.Accessors;
using Dasync.EETypes;
using Dasync.EETypes.Cancellation;
using Dasync.EETypes.Descriptors;
using Dasync.EETypes.Intents;
using Dasync.EETypes.Proxy;
using Dasync.EETypes.Triggers;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.EETypes
{
    public class EETypesNameShortener : ITypeNameShortener
    {
        private static readonly Dictionary<Type, string> _typeToNameMap = new Dictionary<Type, string>();
        private static readonly Dictionary<string, Type> _nameToTypeMap = new Dictionary<string, Type>();

        static EETypesNameShortener()
        {
            RegisterType(typeof(ServiceProxyContext), "proxy");
            RegisterType(typeof(RoutineReference), nameof(RoutineReference));
            RegisterType(typeof(TriggerReference), nameof(TriggerReference));
            RegisterType(typeof(CancellationTokenSourceState), nameof(CancellationTokenSourceState));
            RegisterType(typeof(ServiceId), nameof(ServiceId));
            RegisterType(typeof(MethodId), nameof(MethodId));
            RegisterType(typeof(ContinuationDescriptor), nameof(ContinuationDescriptor));
            RegisterType(typeof(PersistedMethodId), nameof(PersistedMethodId));
            RegisterType(typeof(ResultDescriptor), nameof(ResultDescriptor));
            RegisterType(typeof(ServiceDescriptor), nameof(ServiceDescriptor));
            RegisterType(typeof(CallerDescriptor), nameof(CallerDescriptor));
            RegisterType(typeof(TaskResult), nameof(TaskResult));
            RegisterType(typeof(TransitionDescriptor), nameof(TransitionDescriptor));
            RegisterType(typeof(TransitionType), nameof(TransitionType));
            RegisterType(typeof(ContinueRoutineIntent), nameof(ContinueRoutineIntent));
            RegisterType(typeof(CreateServiceInstanceIntent), nameof(CreateServiceInstanceIntent));
            RegisterType(typeof(ExecuteRoutineIntent), nameof(ExecuteRoutineIntent));
            RegisterType(typeof(SaveStateIntent), nameof(SaveStateIntent));
            RegisterType(typeof(ScheduledActions), nameof(ScheduledActions));
            RegisterType(typeof(IValueContainer), nameof(IValueContainer));
            RegisterType(typeof(ValueContainer.ValueContainer), nameof(ValueContainer.ValueContainer));
            RegisterType(TaskAccessor.WhenAllPromiseType, "WhenAllPromise");
            RegisterType(TaskAccessor.WhenAllPromiseGenericType, "WhenAllPromise`1");
        }

        static void RegisterType(Type type, string shortName)
        {
            _typeToNameMap.Add(type, shortName);
            _nameToTypeMap.Add(shortName, type);
        }

        public bool TryShorten(Type type, out string shortName)
        {
            return _typeToNameMap.TryGetValue(type, out shortName);
        }

        public bool TryExpand(string shortName, out Type type)
        {
            return _nameToTypeMap.TryGetValue(shortName, out type);
        }
    }
}
