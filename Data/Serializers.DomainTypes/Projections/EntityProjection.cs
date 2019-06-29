using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Dasync.Serializers.DomainTypes.Projections
{
    public static class EntityProjection
    {
        public static bool IsProjectionInterface(Type interfaceType) => interfaceType.IsProjectionInterface();

        public static bool IsProjectionInterface<TInterface>() => typeof(TInterface).IsProjectionInterface();

        public static Type GetProjectionType(Type projectionInterfaceType)
            => EntityProjectionTypeBuilder.GetProjectionType(projectionInterfaceType);

        public static Type GetProjectionType<TProjectionInterface>()
            => EntityProjectionTypeBuilder.GetProjectionType(typeof(TProjectionInterface));

        public static object CreateInstance(Type projectionInterfaceType, object tag = null)
        {
            var instance = Activator.CreateInstance(EntityProjectionTypeBuilder.GetProjectionType(projectionInterfaceType));
            SetTag(instance, tag);
            return instance;
        }

        public static TProjectionInterface CreateInstance<TProjectionInterface>(object tag = null)
        {
            var instance = (TProjectionInterface)Activator.CreateInstance(
                EntityProjectionTypeBuilder.GetProjectionType(typeof(TProjectionInterface)));
            SetTag(instance, tag);
            return instance;
        }

        public static TProjectionInterface CreateInstance<TProjectionInterface>(
            Action<Initializer<TProjectionInterface>> initializeAction, object tag = null)
        {
            if (initializeAction == null)
                throw new ArgumentNullException(nameof(initializeAction));

            var instance = (TProjectionInterface)Activator.CreateInstance(
                EntityProjectionTypeBuilder.GetProjectionType(typeof(TProjectionInterface)));

            var initializer = new Initializer<TProjectionInterface>(instance);
            initializeAction(initializer);

            SetTag(instance, tag);

            return instance;
        }

        public static TProjectionInterface SetValue<TProjectionInterface, TValue>(
            TProjectionInterface instance,
            Expression<Func<TProjectionInterface, TValue>> memberAccessExpression,
            TValue value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (memberAccessExpression == null)
                throw new ArgumentNullException(nameof(memberAccessExpression));

            if (!(instance is EntityProjectionBase))
                throw new ArgumentException($"The type '{instance.GetType()}' is not a dynamically generated entity projection.");

            if (!(memberAccessExpression.Body is MemberExpression memberExpression))
                throw new ArgumentException("The expression must be a simple property access like: p => p.Id", nameof(memberAccessExpression));

            var propertyName = memberExpression.Member.Name;
            var backingField = instance.GetType().GetTypeInfo().GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            backingField.SetValue(instance, value);

            return instance;
        }

        public static void SetValue(object instance, string propertyName, object value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            if (!(instance is EntityProjectionBase))
                throw new ArgumentException($"The type '{instance.GetType()}' is not a dynamically generated entity projection.");

            var backingField = instance.GetType().GetTypeInfo().GetField(
                $"<{propertyName}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            backingField.SetValue(instance, value);
        }

        public static void SetTag(object instance, object tag)
        {
            if (!(instance is EntityProjectionBase projectionBase))
                throw new ArgumentException($"The type '{instance.GetType()}' is not a dynamically generated entity projection.");
            projectionBase.SetTag(tag);
        }

        public static object GetTag(object instance)
        {
            if (!(instance is EntityProjectionBase projectionBase))
                throw new ArgumentException($"The type '{instance.GetType()}' is not a dynamically generated entity projection.");
            return projectionBase.GetTag();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed class Initializer<TProjectionInterface>
        {
            private readonly TProjectionInterface _instance;

            public Initializer(TProjectionInterface instance) => _instance = instance;

            [EditorBrowsable(EditorBrowsableState.Never)]
            public sealed class PropertySetter<TValue>
            {
                private readonly TProjectionInterface _instance;
                private readonly Expression<Func<TProjectionInterface, TValue>> _memberAccessExpression;

                public PropertySetter(
                    TProjectionInterface instance,
                    Expression<Func<TProjectionInterface, TValue>> memberAccessExpression)
                {
                    _instance = instance;
                    _memberAccessExpression = memberAccessExpression;
                }

                public void Set(TValue value) => SetValue(_instance, _memberAccessExpression, value);
            }

            public PropertySetter<TValue> Property<TValue>(Expression<Func<TProjectionInterface, TValue>> memberAccessExpression)
                => new PropertySetter<TValue>(_instance, memberAccessExpression);
        }
    }
}
