using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Dasync.ValueContainer;

namespace Dasync.Proxy
{
    public class MethodInvoker : IMethodInvoker
    {
        private IValueContainerFactory _parametersContainerFactory;
        private MethodInfo _methodInfo;
        private DynamicMethod _invoke;

        public MethodInvoker(MethodInfo methodInfo, IValueContainerFactory parametersContainerFactory)
        {
            _methodInfo = methodInfo;
            _parametersContainerFactory = parametersContainerFactory;
            _invoke = BuildInvokeMethod(methodInfo, (IStronglyTypedValueContainer)parametersContainerFactory.Create());
        }

        private static DynamicMethod BuildInvokeMethod(
            MethodInfo method,
            IStronglyTypedValueContainer sampleContainer)
        {
            DynamicMethod dm;
            if (method.DeclaringType.GetTypeInfo().IsInterface)
            {
                dm = new DynamicMethod(method.Name + "!d", method.ReturnType,
                    new Type[] { method.DeclaringType, typeof(IValueContainer) },
                    method.Module);
            }
            else
            {
                dm = new DynamicMethod(method.Name + "!d", method.ReturnType,
                    new Type[] { method.DeclaringType, typeof(IValueContainer) },
                    method.DeclaringType);
            }

            var il = dm.GetILGenerator();

            var paramCount = sampleContainer.GetCount();

            if (paramCount > 0)
            {
                il.DeclareLocal(sampleContainer.GetType());

                il.Emit(OpCodes.Ldarg_1);
                if (sampleContainer.GetType().GetTypeInfo().IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, sampleContainer.GetType());
                }
                else
                {
                    il.Emit(OpCodes.Castclass, sampleContainer.GetType());
                }
                il.Emit(OpCodes.Stloc_0);
            }

            if (!method.IsStatic)
                il.Emit(OpCodes.Ldarg_0);

            for (var i = 0; i < paramCount; i++)
            {
                var member = sampleContainer.GetMember(i);
                if (member is FieldInfo fi)
                {
                    il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ldfld, fi);
                }
                else if (member is PropertyInfo pi)
                {
                    if (sampleContainer.GetType().GetTypeInfo().IsValueType)
                    {
                        il.Emit(OpCodes.Ldloca_S, 0);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldloc_0);
                    }
                    il.Emit(OpCodes.Call, pi.GetMethod);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            if (method.IsAbstract || method.DeclaringType.GetTypeInfo().IsInterface)
                il.Emit(OpCodes.Callvirt, method);
            else
                il.Emit(OpCodes.Call, method);

            il.Emit(OpCodes.Ret);

            return dm;
        }

        public IValueContainer CreateParametersContainer()
        {
#warning Try to use IValueContainerFactory<T> when pre-compile this code
            var container = _parametersContainerFactory.Create();
#warning Pre-compile dynamic code
            var parameters = _methodInfo.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var p = parameters[i];
                if (p.HasDefaultValue)
                    container.SetValue(i, p.DefaultValue);
            }
            return container;
        }

        public Task Invoke(object instance, IValueContainer parameters)
        {
            return (Task)_invoke.Invoke(instance, new object[] { instance, parameters });
        }
    }
}
