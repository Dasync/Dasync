using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dasync.ValueContainer
{
    public class ValueContainerFactory
    {
#warning need value container type cache!
#warning need value container factory!
#warning need value container info!

        public static IValueContainer CreateProxy(object target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            var containerType = ValueContainerTypeBuilder.Build(target.GetType(), GetDelegatedMembers(target.GetType()));
            return (IValueContainer)Activator.CreateInstance(containerType, new object[] { target });
        }

        public static IValueContainer CreateProxy(object target, IEnumerable<MemberInfo> members)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (members == null)
                throw new ArgumentNullException(nameof(members));

#warning Disable readonly members

            var containerType = ValueContainerTypeBuilder.Build(target.GetType(), members);
            return (IValueContainer)Activator.CreateInstance(containerType, new object[] { target });
        }

        public static IValueContainer CreateProxy(object target, IEnumerable<KeyValuePair<string, MemberInfo>> properties)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

#warning Disable readonly members

            var containerType = ValueContainerTypeBuilder.Build(target.GetType(), properties);
            return (IValueContainer)Activator.CreateInstance(containerType, new object[] { target });
        }

        public static IValueContainer Create(IEnumerable<KeyValuePair<string, Type>> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            var containerType = ValueContainerTypeBuilder.Build(properties);
            return (IValueContainer)Activator.CreateInstance(containerType);
        }

        private static IEnumerable<KeyValuePair<string,MemberInfo>> GetDelegatedMembers(Type type)
        {
            foreach (var mi in type.GetMembers(BindingFlags.Instance | BindingFlags.Public))
            {
                var fieldInfo = mi as FieldInfo;
                if (fieldInfo != null)
                {
                    if (!fieldInfo.IsInitOnly)
                        yield return new KeyValuePair<string, MemberInfo>(fieldInfo.Name, fieldInfo);
                    continue;
                }

                var propertyInfo = mi as PropertyInfo;
                if (propertyInfo != null)
                {
                    if (propertyInfo.CanRead && propertyInfo.CanWrite)
                    {
                        MemberInfo resultMi = propertyInfo;
                        if (!propertyInfo.SetMethod.IsPublic)
                        {
                            resultMi = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).First(fi => fi.Name.Contains($"<{propertyInfo.Name}>"));
                        }
                        yield return new KeyValuePair<string, MemberInfo>(propertyInfo.Name, resultMi);
                    }
                    continue;
                }
            }
        }

        public static IValueContainerFactory GetFactory(IEnumerable<KeyValuePair<string, Type>> properties)
        {
#warning Pre-cache
            var containerType = ValueContainerTypeBuilder.Build(properties);
            var factoryType = typeof(ValueContainerFactory<>).MakeGenericType(containerType);
            return (IValueContainerFactory)Activator.CreateInstance(factoryType);
        }

        public static IValueContainerProxyFactory GetProxyFactory(Type delegatedType)
        {
            var properties = GetDelegatedMembers(delegatedType);
#warning Pre-cache
            var containerType = ValueContainerTypeBuilder.Build(delegatedType, properties);
            var factoryType = typeof(ValueContainerProxyFactory<,>).MakeGenericType(delegatedType, containerType);
            return (IValueContainerProxyFactory)Activator.CreateInstance(factoryType);
        }

        public static IValueContainerProxyFactory GetProxyFactory(Type delegatedType, IEnumerable<KeyValuePair<string, MemberInfo>> properties)
        {
#warning Pre-cache
            var containerType = ValueContainerTypeBuilder.Build(delegatedType, properties);
            var factoryType = typeof(ValueContainerProxyFactory<,>).MakeGenericType(delegatedType, containerType);
            return (IValueContainerProxyFactory)Activator.CreateInstance(factoryType);
        }
    }

    public class ValueContainerFactory<T> : IValueContainerFactory, IValueContainerFactory<T> where T : IValueContainer, new()
    {
        public T Create() => new T();

        IValueContainer IValueContainerFactory.Create() => new T();
    }

    public class ValueContainerProxyFactory<TObject, TContainer>
        : IValueContainerProxyFactory,
        IValueContainerProxyFactory<TObject, TContainer>
        where TContainer : IValueContainer
    {
        public IValueContainer Create(object target)
        {
#warning pre-compile
            return (IValueContainer)Activator.CreateInstance(typeof(TContainer), new object[] { target });
        }

        public TContainer Create(TObject target)
        {
#warning pre-compile
            return (TContainer)Activator.CreateInstance(typeof(TContainer), new object[] { target });
        }
    }
}
