using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Dasync.Serializers.DomainTypes.Projections
{
    public class EntityProjectionTypeBuilder
    {
        private static readonly Lazy<ModuleBuilder> _moduleBuilder;
        private static int _typeCounter;
        private static Dictionary<Type, Type> _projectionTypes = new Dictionary<Type, Type>();

        private sealed class Context
        {
            public TypeBuilder Builder;
            public HashSet<Type> ImplementedInterfaces = new HashSet<Type>();
        }

        static EntityProjectionTypeBuilder()
        {
            _moduleBuilder = new Lazy<ModuleBuilder>(() =>
            {
                var thisAssembly = typeof(EntityProjectionTypeBuilder).GetTypeInfo().Assembly;
                var assemblyName = new AssemblyName(thisAssembly.GetName().Name + ".Dynamic");
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                var moduleName = assemblyName.Name + ".dll";
                var module = assemblyBuilder.DefineDynamicModule(moduleName);
                return module;
            },
            LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public static Type GetProjectionType(Type projectionInterfaceType)
        {
            lock (_projectionTypes)
            {
                if (_projectionTypes.TryGetValue(projectionInterfaceType, out var projectionType))
                    return projectionType;
                projectionType = BuildProjectionType(projectionInterfaceType);
                _projectionTypes.Add(projectionInterfaceType, projectionType);
                return projectionType;
            }
        }

        private static Type BuildProjectionType(Type projectionInterfaceType)
        {
            if (!projectionInterfaceType.IsProjectionInterface())
                throw new InvalidOperationException($"The type '{projectionInterfaceType}' cannot be used as a projection interface.");

            var name = projectionInterfaceType.Name;
            if (name.StartsWith("I") && name.Length > 1)
                name = name.Substring(1);
            if (name.EndsWith("View") && name.Length > 4)
                name = name.Substring(0, name.Length - 4);
            else if (name.EndsWith("Projection") && name.Length > 10)
                name = name.Substring(0, name.Length - 10);
            name += $"-projection#{Interlocked.Increment(ref _typeCounter)}";

            var typeBuilder = _moduleBuilder.Value.DefineType(name,
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed,
                parent: typeof(EntityProjectionBase));

            var context = new Context { Builder = typeBuilder };
            ImplementInterface(context, projectionInterfaceType);

            var projectionType = typeBuilder.CreateTypeInfo().AsType();
            return projectionType;
        }

        private static void ImplementInterface(Context context, Type interfaceType)
        {
            if (!context.ImplementedInterfaces.Add(interfaceType))
                return;

            foreach (var subInterfaceType in interfaceType.GetInterfaces())
                ImplementInterface(context, subInterfaceType);

            ImplementInterfaceProperties(context, interfaceType);
        }

        private static void ImplementInterfaceProperties(Context context, Type interfaceType)
        {
            context.Builder.AddInterfaceImplementation(interfaceType);

            foreach (var propertyInfo in interfaceType.GetProperties())
            {
                var backingFieldName = string.Concat("<", propertyInfo.Name, ">k__BackingField");
                var backingFieldBuilder = context.Builder.DefineField(backingFieldName, propertyInfo.PropertyType, FieldAttributes.Private);
                backingFieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new object[0]));

                var getterBuilder = context.Builder.DefineMethod("get_" + propertyInfo.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                    propertyInfo.PropertyType, Type.EmptyTypes);
                getterBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new object[0]));

                var getterIL = getterBuilder.GetILGenerator();
                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, backingFieldBuilder);
                getterIL.Emit(OpCodes.Ret);

                var setterBuilder = context.Builder.DefineMethod("set_" + propertyInfo.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual,
                    null, new Type[] { propertyInfo.PropertyType });
                setterBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes), new object[0]));

                var setterIL = setterBuilder.GetILGenerator();
                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Stfld, backingFieldBuilder);
                setterIL.Emit(OpCodes.Ret);

                var propertyBuilder = context.Builder.DefineProperty(propertyInfo.Name, PropertyAttributes.None, propertyInfo.PropertyType, Type.EmptyTypes);
                propertyBuilder.SetGetMethod(getterBuilder);
                propertyBuilder.SetSetMethod(setterBuilder);

                context.Builder.DefineMethodOverride(getterBuilder, propertyInfo.GetMethod);
            }
        }
    }
}
