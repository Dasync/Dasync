using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Threading;

namespace Dasync.ValueContainer
{
    public static class ValueContainerTypeBuilder
    {
        private static readonly Lazy<ModuleBuilder> _moduleBuilder;
        private static int _typeCounter;

        private struct PropertyDesc
        {
            public string Name;
            public Type Type;
            public MemberInfo Delegate;
        }

        private struct ContainerDesc
        {
            public string ExplicitName;
            public Type DelegatedType;
            public PropertyDesc[] Properties;
        }

        private sealed class ContainerDescEqualityComparer : IEqualityComparer<ContainerDesc>
        {
            public static readonly ContainerDescEqualityComparer Instance = new ContainerDescEqualityComparer();

            private ContainerDescEqualityComparer() { }

            public bool Equals(ContainerDesc x, ContainerDesc y)
            {
                if (!ReferenceEquals(x.DelegatedType, y.DelegatedType))
                    return false;

                if (!string.Equals(x.ExplicitName, y.ExplicitName, StringComparison.Ordinal))
                    return false;

                int propCount;
                if ((propCount = (x.Properties?.Length ?? 0)) != (y.Properties?.Length ?? 0))
                    return false;

                for (var i = 0; i < propCount; i++)
                {
                    var xp = x.Properties[i];
                    var yp = y.Properties[i];

                    if (!string.Equals(xp.Name, yp.Name, StringComparison.Ordinal))
                        return false;

                    if (!ReferenceEquals(xp.Type, yp.Type))
                        return false;

                    if (!ReferenceEquals(xp.Delegate, yp.Delegate))
                        return false;
                }

                return true;
            }

            public int GetHashCode(ContainerDesc desc)
            {
                unchecked
                {
                    int hash = -2128831035;

                    if (desc.DelegatedType != null)
                        hash = (hash * 16777619) ^ desc.DelegatedType.GetHashCode();

                    if (desc.ExplicitName != null)
                        hash = (hash * 16777619) ^ desc.ExplicitName.GetHashCode();

                    var props = desc.Properties;
                    if (props != null)
                    {
                        for (var i = 0; i < props.Length; i++)
                        {
                            var p = props[i];
                            if (p.Name != null)
                                hash = (hash * 16777619) ^ p.Name.GetHashCode();
                            if (p.Type != null)
                                hash = (hash * 16777619) ^ p.Type.GetHashCode();
                            if (p.Delegate != null)
                                hash = (hash * 16777619) ^ p.Delegate.GetHashCode();
                        }
                    }

                    return hash;
                }
            }
        }

        private struct BuiltProperty
        {
            public string Name;
            public Type Type;
            public PropertyBuilder Builder;
            public MethodBuilder Getter;
            public MethodBuilder Setter;
            public FieldBuilder BackingField;
            public KeyValuePair<FieldBuilder, Delegate> CompiledGetter;
            public KeyValuePair<FieldBuilder, Delegate> CompiledSetter;
        }

        private static readonly ConcurrentDictionary<ContainerDesc, Type> _builtTypeMap
            = new ConcurrentDictionary<ContainerDesc, Type>(ContainerDescEqualityComparer.Instance);

        private static readonly Func<ContainerDesc, Type> _buildContainerType = BuildInternal;

        private static readonly ConstructorInfo IndexOutOfRangeExceptionCtor =
            typeof(IndexOutOfRangeException).GetConstructor(Type.EmptyTypes);

        static ValueContainerTypeBuilder()
        {
            _moduleBuilder = new Lazy<ModuleBuilder>(() =>
            {
                var thisAssembly = typeof(ValueContainerTypeBuilder).GetTypeInfo().Assembly;
                var assemblyName = new AssemblyName(thisAssembly.GetName().Name + ".Dynamic");

                var assemblyBuilder =
#if NETFX
                    AppDomain.CurrentDomain.
#else
                    AssemblyBuilder.
#endif
                    DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

                var moduleName = assemblyName.Name + ".dll";

                assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes),
                    new object[0]));

#warning most likely this is not needed at all
#if NETFX
                assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(SecurityRulesAttribute).GetConstructor(new[] { typeof(SecurityRuleSet) }),
                    new object[] { SecurityRuleSet.Level1 }));
#endif

                var module = assemblyBuilder.DefineDynamicModule(moduleName);

                module.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes),
                    new object[0]));

#if NETFX
                module.SetCustomAttribute(new CustomAttributeBuilder(
                    typeof(SecurityRulesAttribute).GetConstructor(new[] { typeof(SecurityRuleSet) }),
                    new object[] { SecurityRuleSet.Level1 }));
#endif

                return module;
            },
            LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public static Type Build(IEnumerable<KeyValuePair<string, Type>> properties, string newTypeName = null)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            return _builtTypeMap.GetOrAdd(
                new ContainerDesc
                {
                    ExplicitName = newTypeName,
                    Properties = properties.Select(
                        kv => new PropertyDesc
                        {
                            Name = kv.Key,
                            Type = kv.Value
                        })
                        .ToArray()
                },
                _buildContainerType);
        }

        public static Type Build(Type delegatedType, IEnumerable<MemberInfo> delegatedProperties, string newTypeName = null)
        {
            if (delegatedType == null)
                throw new ArgumentNullException(nameof(delegatedType));

            return _builtTypeMap.GetOrAdd(
                new ContainerDesc
                {
                    ExplicitName = newTypeName,
                    DelegatedType = delegatedType,
                    Properties = delegatedProperties.Select(
                        mi => new PropertyDesc
                        {
                            Name = mi.Name,
                            Delegate = mi
                        })
                        .ToArray()
                },
                _buildContainerType);
        }

        public static Type Build(Type delegatedType, IEnumerable<KeyValuePair<string, MemberInfo>> delegatedProperties, string newTypeName = null)
        {
            if (delegatedType == null)
                throw new ArgumentNullException(nameof(delegatedType));

            return _builtTypeMap.GetOrAdd(
                new ContainerDesc
                {
                    ExplicitName = newTypeName,
                    DelegatedType = delegatedType,
                    Properties = delegatedProperties.Select(
                        p => new PropertyDesc
                        {
                            Name = p.Key,
                            Delegate = p.Value
                        })
                        .ToArray()
                },
                _buildContainerType);
        }

        private static Type BuildInternal(ContainerDesc desc)
        {
            if (desc.DelegatedType == null &&
                string.IsNullOrEmpty(desc.ExplicitName) &&
                (desc.Properties?.Length ?? 0) == 0)
                return typeof(EmptyValueContainer);

            var newTypeName = desc.ExplicitName;
            if (string.IsNullOrEmpty(newTypeName))
                newTypeName = GenerateNewTypeName();

            var typeBuilder = _moduleBuilder.Value.DefineType(newTypeName,
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed |
                TypeAttributes.SequentialLayout | TypeAttributes.Serializable,
                typeof(ValueType));

            var namesField = typeBuilder.DefineField("Names", typeof(string[]), FieldAttributes.Public | FieldAttributes.Static);
            var typesField = typeBuilder.DefineField("Types", typeof(Type[]), FieldAttributes.Public | FieldAttributes.Static);
            var membersField = typeBuilder.DefineField("Members", typeof(MemberInfo[]), FieldAttributes.Public | FieldAttributes.Static);

            FieldBuilder sourceField = null;
            if (desc.DelegatedType != null)
            {
                if (desc.DelegatedType.GetTypeInfo().IsValueType || !desc.DelegatedType.GetTypeInfo().IsVisible)
                    sourceField = typeBuilder.DefineField("@source", typeof(object), FieldAttributes.Private);
                else
                    sourceField = typeBuilder.DefineField("@source", desc.DelegatedType, FieldAttributes.Private);

                var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { sourceField.FieldType });
                var ctorIL = ctor.GetILGenerator();
                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Ldarg_1);
                ctorIL.Emit(OpCodes.Stfld, sourceField);
                ctorIL.Emit(OpCodes.Ret);
            }

            var builtProperties = ImplementProperties(typeBuilder, desc.Properties, desc.DelegatedType, sourceField);

            typeBuilder.AddInterfaceImplementation(typeof(IValueContainer));
            Implement_IValueContainer_GetCount(typeBuilder, builtProperties.Count);
            Implement_IValueContainer_GetName(typeBuilder, namesField);
            Implement_IValueContainer_GetType(typeBuilder, typesField);
            Implement_IValueContainer_GetValue(typeBuilder, builtProperties);
            Implement_IValueContainer_SetValue(typeBuilder, builtProperties);

            typeBuilder.AddInterfaceImplementation(typeof(IStronglyTypedValueContainer));
            Implement_IStronglyTypedValueContainer_GetMember(typeBuilder, builtProperties, membersField);

#if NETFX
            var valueContainerType = typeBuilder.CreateType();
#else
            var valueContainerType = typeBuilder.CreateTypeInfo().AsType();
#endif

            valueContainerType.GetField("Names", BindingFlags.Static | BindingFlags.Public)
                .SetValue(null, builtProperties.Select(p => p.Name).ToArray());

            valueContainerType.GetField("Types", BindingFlags.Static | BindingFlags.Public)
                .SetValue(null, builtProperties.Select(p => p.Type).ToArray());

            valueContainerType.GetField("Members", BindingFlags.Static | BindingFlags.Public)
                .SetValue(null, builtProperties.Select(p => valueContainerType.GetProperty(p.Builder.Name)).ToArray());

            foreach (var prop in builtProperties)
            {
                if (prop.CompiledGetter.Key != null)
                    valueContainerType
                        .GetField(prop.CompiledGetter.Key.Name, BindingFlags.Static | BindingFlags.NonPublic)
                        .SetValue(null, prop.CompiledGetter.Value);

                if (prop.CompiledSetter.Key != null)
                    valueContainerType
                        .GetField(prop.CompiledSetter.Key.Name, BindingFlags.Static | BindingFlags.NonPublic)
                        .SetValue(null, prop.CompiledSetter.Value);
            }

            return valueContainerType;
        }

        private static string GenerateNewTypeName()
        {
            var index = Interlocked.Increment(ref _typeCounter);
            return "ValueContainer!" + index;
        }

        private static string PropertyNameToBackingFieldName(string propertyName)
        {
            var fieldName = string.Concat("_", char.ToLowerInvariant(propertyName[0]), propertyName.Substring(1));
#if NETFX
            return string.Intern(fieldName);
#else
            return fieldName;
#endif
        }

        private static List<BuiltProperty> ImplementProperties(TypeBuilder typeBuilder, IEnumerable<PropertyDesc> properties, Type delegatedType, FieldBuilder sourceField)
        {
            var props = new List<BuiltProperty>();

            foreach (var propDesc in properties)
            {
                var propertyIndex = props.Count;
                var propertyName = propDesc.Name;
                var propertyType = propDesc.Type;

                if (string.IsNullOrEmpty(propertyName))
                    throw new ArgumentException($"The property at index {propertyIndex} must have a name", nameof(properties));

                FieldBuilder backingField = null;
                FieldInfo delegatedField = null;
                PropertyInfo delegatedProperty = null;
                bool hasRestrictedAccess = false;

                if (propDesc.Delegate == null)
                {
                    var backingFieldName = PropertyNameToBackingFieldName(propertyName);
#warning Why not to use public fields?
                    backingField = typeBuilder.DefineField(backingFieldName, propertyType, FieldAttributes.Private);
                }
                else
                {
                    delegatedField = propDesc.Delegate as FieldInfo;
                    if (delegatedField != null)
                    {
                        propertyType = delegatedField.FieldType;
                        hasRestrictedAccess = !delegatedField.IsPublic;
                    }
                    else
                    {
                        delegatedProperty = (PropertyInfo)propDesc.Delegate;
                        propertyType = delegatedProperty.PropertyType;
#warning Update hasRestrictedAccess
                    }
                }

                var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, Type.EmptyTypes);

                var builtProperty = new BuiltProperty
                {
                    Name = propertyName,
                    Type = propertyType,
                    Builder = propertyBuilder,
                    BackingField = backingField
                };

                var getter = typeBuilder.DefineMethod("get_" + propertyName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                    propertyType, Type.EmptyTypes);
                {
#warning Check hasRestrictedAccess first?
                    var getterIL = getter.GetILGenerator();
                    if (backingField != null)
                    {
                        getterIL.Emit(OpCodes.Ldarg_0); // load 'this'
                        getterIL.Emit(OpCodes.Ldfld, backingField);
                        getterIL.Emit(OpCodes.Ret);
                    }
                    else if (sourceField.FieldType == typeof(object) || hasRestrictedAccess)
                    {
                        var getterAccessor =
                            delegatedField != null
                            ? MemberAccessor.GetGetter(delegatedField)
                            : MemberAccessor.GetGetter(delegatedProperty);

                        var getterStaticField = typeBuilder.DefineField(
                            "get@" + propertyName,
                            getterAccessor.GetType(),
                            FieldAttributes.Private | FieldAttributes.Static);

                        getterIL.Emit(OpCodes.Ldsfld, getterStaticField);
                        getterIL.Emit(OpCodes.Ldarg_0); // load 'this'
                        getterIL.Emit(OpCodes.Ldfld, sourceField); // load '@source'
                        getterIL.Emit(OpCodes.Callvirt, getterAccessor.GetType().GetMethod(nameof(Func<object>.Invoke)));
                        getterIL.Emit(OpCodes.Ret);

                        builtProperty.CompiledGetter = new KeyValuePair<FieldBuilder, Delegate>(getterStaticField, getterAccessor);
                    }
                    else
                    {
                        getterIL.Emit(OpCodes.Ldarg_0); // load 'this'
                        getterIL.Emit(OpCodes.Ldfld, sourceField); // load '@source'

                        if (delegatedType.GetTypeInfo().IsValueType)
                            getterIL.Emit(OpCodes.Unbox, delegatedType); // load pointer to boxed value

                        if (delegatedField != null)
                            getterIL.Emit(OpCodes.Ldfld, delegatedField);
                        else
                            getterIL.Emit(OpCodes.Call, delegatedProperty.GetGetMethod());

                        getterIL.Emit(OpCodes.Ret);
                    }
                }
                propertyBuilder.SetGetMethod(getter);
                builtProperty.Getter = getter;

                var setter = typeBuilder.DefineMethod("set_" + propertyName,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                    null, new Type[] { propertyType });
                {
                    var setterIL = setter.GetILGenerator();
#warning Check hasRestrictedAccess first?
                    if (backingField != null)
                    {
                        setterIL.Emit(OpCodes.Ldarg_0); // load 'this'
                        setterIL.Emit(OpCodes.Ldarg_1); // load 'value'
                        setterIL.Emit(OpCodes.Stfld, backingField);
                        setterIL.Emit(OpCodes.Ret);
                    }
                    else if (sourceField.FieldType == typeof(object) || hasRestrictedAccess)
                    {
                        var setterAccessor =
                            delegatedField != null
                            ? MemberAccessor.GetSetter(delegatedField)
                            : MemberAccessor.GetSetter(delegatedProperty);

                        var setterStaticField = typeBuilder.DefineField(
                            "set@" + propertyName,
                            setterAccessor.GetType(),
                            FieldAttributes.Private | FieldAttributes.Static);

                        setterIL.Emit(OpCodes.Ldsfld, setterStaticField);
                        setterIL.Emit(OpCodes.Ldarg_0); // load 'this'
                        setterIL.Emit(OpCodes.Ldfld, sourceField); // load '@source'
                        setterIL.Emit(OpCodes.Ldarg_1); // load 'value'
                        setterIL.Emit(OpCodes.Callvirt, setterAccessor.GetType().GetMethod(nameof(Action.Invoke)));
                        setterIL.Emit(OpCodes.Ret);

                        builtProperty.CompiledSetter = new KeyValuePair<FieldBuilder, Delegate>(setterStaticField, setterAccessor);
                    }
                    else
                    {
                        setterIL.Emit(OpCodes.Ldarg_0); // load 'this'
                        setterIL.Emit(OpCodes.Ldfld, sourceField); // load '@source'

                        if (delegatedType.GetTypeInfo().IsValueType)
                            setterIL.Emit(OpCodes.Unbox, delegatedType); // load pointer to boxed value

                        if (delegatedField != null)
                        {
                            setterIL.Emit(OpCodes.Ldarg_1); // load 'value'
                            setterIL.Emit(OpCodes.Stfld, delegatedField);
                        }
                        else
                        {
                            setterIL.Emit(OpCodes.Ldarg_1); // load 'value'
                            setterIL.Emit(OpCodes.Call, delegatedProperty.GetSetMethod());
                        }

                        setterIL.Emit(OpCodes.Ret);
                    }
                }
                propertyBuilder.SetSetMethod(setter);
                builtProperty.Setter = setter;

                props.Add(builtProperty);
            }

            return props;
        }

        private static void Implement_IValueContainer_GetCount(TypeBuilder typeBuilder, int propertyCount)
        {
            var methodInfo = typeof(IValueContainer).GetMethod(nameof(IValueContainer.GetCount));

            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(int), Type.EmptyTypes);

            var methodIL = methodBuilder.GetILGenerator();
            methodIL.Emit(OpCodes.Ldc_I4, propertyCount);
            methodIL.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        private static void Implement_IValueContainer_GetName(TypeBuilder typeBuilder, FieldBuilder namesField)
        {
            var methodInfo = typeof(IValueContainer).GetMethod(nameof(IValueContainer.GetName));

            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(string), new[] { typeof(int) });

            var methodIL = methodBuilder.GetILGenerator();
            methodIL.Emit(OpCodes.Ldsfld, namesField);
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Ldelem_Ref);
            methodIL.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        private static void Implement_IValueContainer_GetType(TypeBuilder typeBuilder, FieldBuilder typesField)
        {
            var methodInfo = typeof(IValueContainer).GetMethod(nameof(IValueContainer.GetType));

            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(Type), new[] { typeof(int) });

            var methodIL = methodBuilder.GetILGenerator();
            methodIL.Emit(OpCodes.Ldsfld, typesField);
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Ldelem_Ref);
            methodIL.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        private static void Implement_IValueContainer_GetValue(TypeBuilder typeBuilder, List<BuiltProperty> builtProperties)
        {
            var methodInfo = typeof(IValueContainer).GetMethod(nameof(IValueContainer.GetValue));

            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(object), new[] { typeof(int) });

            var methodIL = methodBuilder.GetILGenerator();

            var jumpTable = new Label[builtProperties.Count];
            for (var i = 0; i < jumpTable.Length; i++)
                jumpTable[i] = methodIL.DefineLabel();

            var defaultCase = methodIL.DefineLabel();
            methodIL.Emit(OpCodes.Ldarg_1); // load 'index'
            methodIL.Emit(OpCodes.Switch, jumpTable);
            methodIL.Emit(OpCodes.Br, defaultCase);

            for (var i = 0; i < builtProperties.Count; i++)
            {
                var prop = builtProperties[i];

                methodIL.MarkLabel(jumpTable[i]);
                methodIL.Emit(OpCodes.Ldarg_0); // load 'this'

                if (prop.BackingField == null)
                {
                    methodIL.Emit(OpCodes.Call, prop.Getter); // call property getter
                }
                else
                {
                    methodIL.Emit(OpCodes.Ldfld, prop.BackingField); // load value from the field
                }

                if (prop.Type.GetTypeInfo().IsValueType)
                {
                    methodIL.Emit(OpCodes.Box, prop.Type); // box value type
                }

                methodIL.Emit(OpCodes.Ret);
            }

            methodIL.MarkLabel(defaultCase);
            methodIL.Emit(OpCodes.Newobj, IndexOutOfRangeExceptionCtor);
            methodIL.Emit(OpCodes.Throw);

            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        private static void Implement_IValueContainer_SetValue(TypeBuilder typeBuilder, List<BuiltProperty> builtProperties)
        {
            var methodInfo = typeof(IValueContainer).GetMethod(nameof(IValueContainer.SetValue));

            var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(void), new[] { typeof(int), typeof(object) });

            var methodIL = methodBuilder.GetILGenerator();

            var jumpTable = new Label[builtProperties.Count];
            for (var i = 0; i < jumpTable.Length; i++)
                jumpTable[i] = methodIL.DefineLabel();

            var defaultCase = methodIL.DefineLabel();
            methodIL.Emit(OpCodes.Ldarg_1); // load 'index'
            methodIL.Emit(OpCodes.Switch, jumpTable);
            methodIL.Emit(OpCodes.Br, defaultCase);

            for (var i = 0; i < builtProperties.Count; i++)
            {
                var prop = builtProperties[i];

                methodIL.MarkLabel(jumpTable[i]);
                methodIL.Emit(OpCodes.Ldarg_0); // load 'this'
                methodIL.Emit(OpCodes.Ldarg_2); // load 'value'

                if (prop.Type.GetTypeInfo().IsValueType)
                {
                    methodIL.Emit(OpCodes.Unbox_Any, prop.Type); // unbox value type
                }
                else
                {
                    methodIL.Emit(OpCodes.Castclass, prop.Type); // cast to type
                }

                if (prop.BackingField == null)
                {
                    methodIL.Emit(OpCodes.Call, prop.Setter); // call property setter
                }
                else
                {
                    methodIL.Emit(OpCodes.Stfld, prop.BackingField); // store value in the field
                }

                methodIL.Emit(OpCodes.Ret);
            }

            methodIL.MarkLabel(defaultCase);
            methodIL.Emit(OpCodes.Newobj, IndexOutOfRangeExceptionCtor);
            methodIL.Emit(OpCodes.Throw);

            typeBuilder.DefineMethodOverride(methodBuilder, methodInfo);
        }

        private static void Implement_IStronglyTypedValueContainer_GetMember(TypeBuilder typeBuilder,
            List<BuiltProperty> builtProperties, FieldInfo membersField)
        {
            var m = typeBuilder.DefineMethod(
                nameof(IStronglyTypedValueContainer.GetMember),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeof(MemberInfo), new[] { typeof(int) });

            var il = m.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, membersField);
            il.Emit(OpCodes.Ldarg_1); // load 'index'
            il.Emit(OpCodes.Ldelem_Ref);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(m, typeof(IStronglyTypedValueContainer).GetMethod(nameof(IStronglyTypedValueContainer.GetMember)));
        }
    }
}
