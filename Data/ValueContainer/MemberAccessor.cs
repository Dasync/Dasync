using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Dasync.ValueContainer
{
    internal static class MemberAccessor
    {
        private static readonly string DynamicMethodPrefix = string.Concat("Dasync.ValueContainer.", nameof(MemberAccessor), "!");

        private static readonly ConcurrentDictionary<FieldInfo, Delegate> _fieldGetters = new ConcurrentDictionary<FieldInfo, Delegate>();
        private static readonly ConcurrentDictionary<FieldInfo, Delegate> _fieldSetters = new ConcurrentDictionary<FieldInfo, Delegate>();
        private static readonly ConcurrentDictionary<PropertyInfo, Delegate> _propertyGetters = new ConcurrentDictionary<PropertyInfo, Delegate>();
        private static readonly ConcurrentDictionary<PropertyInfo, Delegate> _propertySetters = new ConcurrentDictionary<PropertyInfo, Delegate>();

        public static Delegate GetGetter(FieldInfo fieldInfo)
        {
            return _fieldGetters.GetOrAdd(fieldInfo, fi =>
            {
                var getter = new DynamicMethod(GetGetterName(fi), fi.FieldType, new[] { typeof(object) }, true);
                var getterIL = getter.GetILGenerator();
                getterIL.Emit(OpCodes.Ldarg_0);
                Type t;
                getterIL.Emit(fi.DeclaringType.GetTypeInfo().IsValueType ? OpCodes.Unbox : OpCodes.Castclass, fi.DeclaringType);
                getterIL.Emit(OpCodes.Ldfld, fi);
                getterIL.Emit(OpCodes.Ret);
                return getter.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(object), fi.FieldType));
            });
        }

        public static Delegate GetSetter(FieldInfo fieldInfo)
        {
            return _fieldSetters.GetOrAdd(fieldInfo, fi =>
            {
                var setter = new DynamicMethod(GetSetterName(fi), typeof(void), new[] { typeof(object), fi.FieldType }, true);
                var setterIL = setter.GetILGenerator();
                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(fi.DeclaringType.GetTypeInfo().IsValueType ? OpCodes.Unbox : OpCodes.Castclass, fi.DeclaringType);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Stfld, fi);
                setterIL.Emit(OpCodes.Ret);
                return setter.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(object), fi.FieldType));
            });
        }

        public static Delegate GetGetter(PropertyInfo propertyInfo)
        {
            return _propertyGetters.GetOrAdd(propertyInfo, pi =>
            {
                if (!pi.CanRead)
                    throw new InvalidOperationException(
                        $"Cannot create a getter method for non-readable property" +
                        $" '{pi.Name}' of type '{pi.DeclaringType.FullName}'.");

                var getter = new DynamicMethod(GetGetterName(pi), pi.PropertyType, new[] { typeof(object) }, true);
                var getterIL = getter.GetILGenerator();
                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(pi.DeclaringType.GetTypeInfo().IsValueType ? OpCodes.Unbox : OpCodes.Castclass, pi.DeclaringType);
                getterIL.Emit(OpCodes.Call, pi.GetGetMethod(nonPublic: true));
                getterIL.Emit(OpCodes.Ret);
                return getter.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(object), pi.PropertyType));
            });
        }

        public static Delegate GetSetter(PropertyInfo propertyInfo)
        {
            return _propertySetters.GetOrAdd(propertyInfo, pi =>
            {
                if (!pi.CanWrite)
                    throw new InvalidOperationException(
                        $"Cannot create a setter method for non-writable property" +
                        $" '{pi.Name}' of type '{pi.DeclaringType.FullName}'.");

                var setter = new DynamicMethod(GetSetterName(pi), typeof(void), new[] { typeof(object), pi.PropertyType }, true);
                var setterIL = setter.GetILGenerator();
                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(pi.DeclaringType.GetTypeInfo().IsValueType ? OpCodes.Unbox : OpCodes.Castclass, pi.DeclaringType);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Call, pi.GetSetMethod(nonPublic: true));
                setterIL.Emit(OpCodes.Ret);
                return setter.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(object), pi.PropertyType));
            });
        }

        private static string GetCommonPrefix(MemberInfo mi) => DynamicMethodPrefix + mi.DeclaringType.FullName + "$";

        private static string GetGetterName(MemberInfo mi) => string.Concat(GetCommonPrefix(mi), mi.Name, "@get");

        private static string GetSetterName(MemberInfo mi) => string.Concat(GetCommonPrefix(mi), mi.Name, "@set");
    }
}
