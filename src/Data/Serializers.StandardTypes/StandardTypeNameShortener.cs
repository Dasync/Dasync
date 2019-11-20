using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Serialization;

namespace Dasync.Serializers.StandardTypes
{
    public class StandardTypeNameShortener : ITypeNameShortener
    {
        private static readonly Dictionary<Type, string> _typeToNameMap = new Dictionary<Type, string>();
        private static readonly Dictionary<string, Type> _nameToTypeMap = new Dictionary<string, Type>();

        static StandardTypeNameShortener()
        {
#warning process nullables and arrays differently
            RegisterType(typeof(Type), "Type");
            RegisterType(typeof(void), "void");
            RegisterType(typeof(object), "object");
            RegisterType(typeof(byte), "byte");
            RegisterType(typeof(byte?), "byte?");
            RegisterType(typeof(sbyte), "sbyte");
            RegisterType(typeof(sbyte?), "sbyte?");
            RegisterType(typeof(short), "short");
            RegisterType(typeof(short?), "short?");
            RegisterType(typeof(ushort), "ushort");
            RegisterType(typeof(ushort?), "ushort?");
            RegisterType(typeof(int), "int");
            RegisterType(typeof(int?), "int?");
            RegisterType(typeof(uint), "uint");
            RegisterType(typeof(uint?), "uint?");
            RegisterType(typeof(long), "long");
            RegisterType(typeof(long?), "long?");
            RegisterType(typeof(ulong), "ulong");
            RegisterType(typeof(ulong?), "ulong?");
            RegisterType(typeof(float), "float");
            RegisterType(typeof(float?), "float?");
            RegisterType(typeof(double), "double");
            RegisterType(typeof(double?), "double?");
            RegisterType(typeof(decimal), "decimal");
            RegisterType(typeof(decimal?), "decimal?");
            RegisterType(typeof(Guid), "Guid");
            RegisterType(typeof(Guid?), "Guid?");
            RegisterType(typeof(TimeSpan), "TimeSpan");
            RegisterType(typeof(TimeSpan?), "TimeSpan?");
            RegisterType(typeof(DateTime), "DateTime");
            RegisterType(typeof(DateTime?), "DateTime?");
            RegisterType(typeof(DateTimeOffset), "DateTimeOffset");
            RegisterType(typeof(DateTimeOffset?), "DateTimeOffset?");
            RegisterType(typeof(string), "string");
            RegisterType(typeof(Uri), "Uri");
            RegisterType(typeof(IDisposable), "IDisposable");
            RegisterType(typeof(Task), "Task");
            RegisterType(typeof(Task<>), "Task`1");
            RegisterType(typeof(TaskAwaiter), "TaskAwaiter");
            RegisterType(typeof(TaskAwaiter<>), "TaskAwaiter`1");
            RegisterType(typeof(Task).GetNestedType("DelayPromise", BindingFlags.NonPublic), "DelayPromise");
            RegisterType(typeof(ConfiguredTaskAwaitable), "ConfiguredTaskAwaitable");
            RegisterType(typeof(ConfiguredTaskAwaitable<>), "ConfiguredTaskAwaitable`1");
            RegisterType(typeof(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter), "ConfiguredTaskAwaiter");
            RegisterType(typeof(ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter), "ConfiguredTaskAwaiter`1");
            RegisterType(typeof(YieldAwaitable), "YieldAwaitable");
            RegisterType(typeof(YieldAwaitable.YieldAwaiter), "YieldAwaiter");
            RegisterType(typeof(Version), "Version");
            RegisterType(typeof(CancellationToken), nameof(CancellationToken));
            RegisterType(typeof(CancellationTokenSource), nameof(CancellationTokenSource));
            RegisterType(typeof(List<>), "List`1");
            RegisterType(typeof(Hashtable), "Hashtable");
            RegisterType(typeof(HashSet<>), "HashSet`1");
            RegisterType(typeof(Dictionary<,>), "Dictionary`2");
            RegisterType(typeof(KeyValuePair<,>), "KeyValuePair`2");
            RegisterType(typeof(SortedList), "SortedList");
            RegisterType(typeof(SortedList<,>), "SortedList`2");
            RegisterType(typeof(Stack), "Stack");
            RegisterType(typeof(Stack<>), "Stack`1");
            RegisterType(typeof(Queue), "Queue");
            RegisterType(typeof(Queue<>), "Queue`1");
            RegisterType(typeof(IList), "IList");
            RegisterType(typeof(IList<>), "IList`1");
            RegisterType(typeof(ICollection), "ICollection");
            RegisterType(typeof(ICollection<>), "ICollection`1");
            RegisterType(typeof(IReadOnlyCollection<>), "IReadOnlyCollection`1");
            RegisterType(typeof(IReadOnlyList<>), "IReadOnlyList`1");
            RegisterType(typeof(IEnumerable), "IEnumerable");
            RegisterType(typeof(IEnumerable<>), "IEnumerable`1");
            RegisterType(typeof(IEnumerator), "IEnumerator");
            RegisterType(typeof(IEnumerator<>), "IEnumerator`1");
            RegisterType(typeof(IDictionary), "IDictionary");
            RegisterType(typeof(IDictionary<,>), "IDictionary`2");
            RegisterType(typeof(EventArgs), "EventArgs");

            RegisterType(typeof(Exception), nameof(Exception));
            RegisterType(typeof(ArgumentNullException), nameof(ArgumentNullException));
            RegisterType(typeof(ArgumentException), nameof(ArgumentException));
            RegisterType(typeof(InvalidOperationException), nameof(InvalidOperationException));
            RegisterType(typeof(AggregateException), nameof(AggregateException));
            RegisterType(typeof(ApplicationException), nameof(ApplicationException));
            RegisterType(typeof(ArgumentOutOfRangeException), nameof(ArgumentOutOfRangeException));
            RegisterType(typeof(ArithmeticException), nameof(ArithmeticException));
            RegisterType(typeof(DivideByZeroException), nameof(DivideByZeroException));
            RegisterType(typeof(IndexOutOfRangeException), nameof(IndexOutOfRangeException));
            RegisterType(typeof(InvalidCastException), nameof(InvalidCastException));
            RegisterType(typeof(InvalidTimeZoneException), nameof(InvalidTimeZoneException));
            RegisterType(typeof(KeyNotFoundException), nameof(KeyNotFoundException));
            RegisterType(typeof(NotFiniteNumberException), nameof(NotFiniteNumberException));
            RegisterType(typeof(NotImplementedException), nameof(NotImplementedException));
            RegisterType(typeof(NotSupportedException), nameof(NotSupportedException));
            RegisterType(typeof(NullReferenceException), nameof(NullReferenceException));
            RegisterType(typeof(ObjectDisposedException), nameof(ObjectDisposedException));
            RegisterType(typeof(OperationCanceledException), nameof(OperationCanceledException));
            RegisterType(typeof(OutOfMemoryException), nameof(OutOfMemoryException));
            RegisterType(typeof(OverflowException), nameof(OverflowException));
            RegisterType(typeof(SystemException), nameof(SystemException));
            RegisterType(typeof(TargetException), nameof(TargetException));
            RegisterType(typeof(TaskCanceledException), nameof(TaskCanceledException));
            RegisterType(typeof(ThreadAbortException), nameof(ThreadAbortException));
            RegisterType(typeof(TimeoutException), nameof(TimeoutException));
            RegisterType(typeof(UnauthorizedAccessException), nameof(UnauthorizedAccessException));
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
