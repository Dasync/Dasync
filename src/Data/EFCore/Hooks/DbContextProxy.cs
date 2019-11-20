using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Dasync.EntityFrameworkCore.Hooks
{
    public class DbContextProxy
    {
        private static readonly Lazy<ModuleBuilder> ModuleBuilder;

        static DbContextProxy()
        {
            ModuleBuilder = new Lazy<ModuleBuilder>(() =>
            {
                var assemblyName = new AssemblyName("Dasync.EntityFrameworkCore.Proxy");
                var assemblyBuilder =
#if NETFX
                    AppDomain.CurrentDomain.
#else
                    AssemblyBuilder.
#endif
                    DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

                var moduleName = assemblyName.Name + ".dll";
                return assemblyBuilder.DefineDynamicModule(moduleName);
            },
            LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public static Type CreateDbContextProxyType(Type dbContextType)
        {
            var proxyTypeFullName = dbContextType.FullName + "!proxy";
            while (ModuleBuilder.Value.GetType(proxyTypeFullName, throwOnError: false, ignoreCase: false) != null)
                proxyTypeFullName += "′";

            var typeBuilder = ModuleBuilder.Value.DefineType(proxyTypeFullName,
                TypeAttributes.Class | TypeAttributes.Public, parent: dbContextType);

            typeBuilder.AddInterfaceImplementation(typeof(IDbContextProxy));

            CopyConstructors(typeBuilder, dbContextType);
            Implement_get_Context(typeBuilder);
            Implement_OnModelCreatingCallback(typeBuilder, dbContextType);

            return typeBuilder.CreateTypeInfo().AsType();
        }

        private static void CopyConstructors(TypeBuilder typeBuilder, Type baseType)
        {
            var ctors = baseType.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var ctor in ctors)
            {
                var parameterTypes = ctor.GetParameters().Select(pi => pi.ParameterType).ToArray();
                var ctorCopy = typeBuilder.DefineConstructor(
                    ctor.Attributes, ctor.CallingConvention, parameterTypes);

                var il = ctorCopy.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                for (var i = 1; i <= parameterTypes.Length; i++)
                    il.Emit(OpCodes.Ldarg_S, i);
                il.Emit(OpCodes.Call, ctor);
                il.Emit(OpCodes.Ret);
            }
        }

        private static void Implement_get_Context(TypeBuilder typeBuilder)
        {
            var interfaceProperty = typeof(IDbContextProxy).GetProperty(nameof(IDbContextProxy.Context));

            var contextProperty = typeBuilder.DefineProperty(
                interfaceProperty.Name, PropertyAttributes.None,
                interfaceProperty.PropertyType, null);

            var getMethod = typeBuilder.DefineMethod(
                "get_" + interfaceProperty.Name,
                MethodAttributes.SpecialName | MethodAttributes.Public | MethodAttributes.Virtual,
                interfaceProperty.PropertyType, null);
            {
                var il = getMethod.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ret);
            }
            contextProperty.SetGetMethod(getMethod);
            typeBuilder.DefineMethodOverride(getMethod, interfaceProperty.GetMethod);
        }

        private static void Implement_OnModelCreatingCallback(TypeBuilder typeBuilder, Type baseType)
        {
            var interfaceProperty = typeof(IDbContextProxy).GetProperty(nameof(IDbContextProxy.OnModelCreatingCallback));

            var callbackField = typeBuilder.DefineField("_" + interfaceProperty.Name, interfaceProperty.PropertyType, FieldAttributes.Private);
            var callbackProperty = typeBuilder.DefineProperty(interfaceProperty.Name, PropertyAttributes.None, interfaceProperty.PropertyType, null);

            var getMethod = typeBuilder.DefineMethod(
                "get_" + interfaceProperty.Name,
                MethodAttributes.SpecialName | MethodAttributes.Public | MethodAttributes.Virtual,
                interfaceProperty.PropertyType, null);
            {
                var il = getMethod.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, callbackField);
                il.Emit(OpCodes.Ret);
            }
            callbackProperty.SetGetMethod(getMethod);
            typeBuilder.DefineMethodOverride(getMethod, interfaceProperty.GetMethod);

            var setMethod = typeBuilder.DefineMethod(
                "set_" + interfaceProperty.Name,
                MethodAttributes.SpecialName | MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(void), new Type[] { interfaceProperty.PropertyType });
            {
                var il = setMethod.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, callbackField);
                il.Emit(OpCodes.Ret);
            }
            callbackProperty.SetSetMethod(setMethod);
            typeBuilder.DefineMethodOverride(setMethod, interfaceProperty.SetMethod);

            //-----------------------------------------------------------------------------

            var onModelCreatingMethodInfo = baseType.GetMethod("OnModelCreating", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var parameterTypes = onModelCreatingMethodInfo.GetParameters().Select(p => p.ParameterType).ToArray();

            var onModelCreatingMethod = typeBuilder.DefineMethod(
                onModelCreatingMethodInfo.Name,
                MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                onModelCreatingMethodInfo.ReturnType,
                parameterTypes);
            {
                var invokeMethodInfo = typeof(Action<>).MakeGenericType(parameterTypes).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance);

                var il = onModelCreatingMethod.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0); // this
                il.Emit(OpCodes.Ldarg_1); // modelBuilder
                il.Emit(OpCodes.Call, onModelCreatingMethodInfo); // base

                var label1 = il.DefineLabel();
                var label2 = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_0); // this
                il.Emit(OpCodes.Ldfld, callbackField);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue_S, label1);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Br_S, label2);
                il.MarkLabel(label1);
                il.Emit(OpCodes.Ldarg_1); // modelBuilder
                il.Emit(OpCodes.Callvirt, invokeMethodInfo);

                il.MarkLabel(label2);
                il.Emit(OpCodes.Ret);
            }
        }
    }
}
