using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dasync.EETypes;

namespace Dasync.Fabric.InMemory
{
    public class InMemoryDataStore
    {
        public class Message : Dictionary<string, string>
        {
            public DateTime? DeliverAt;

            public int Size
            {
                get
                {
                    var size = 0;
                    foreach (var pair in this)
                    {
                        if (pair.Key != null)
                            size += pair.Key.Length;
                        if (pair.Value != null)
                            size += pair.Value.Length;
                    }
                    return size;
                }
            }
        }

        public class RoutineStateRecord
        {
            public string ETag;
            public string Id;
            public string State;
            public string Continuation;
            public string Result;
            public TaskCompletionSource<string> Completion;
        }

        public class ServiceStateRecord
        {
            public string ETag;
            public ServiceId Id;
            public string State;
        }

        public int Id { get; private set; }

        public int RoutineCounter;

        public Action<Message> ScheduleMessage { get; private set; }

        public Dictionary<string, RoutineStateRecord> Routines { get; } =
            new Dictionary<string, RoutineStateRecord>();

        public Dictionary<string, ServiceStateRecord> Services { get; } =
            new Dictionary<string, ServiceStateRecord>();

        private static Dictionary<int, InMemoryDataStore> _dataStoreMap =
            new Dictionary<int, InMemoryDataStore>();

        public RoutineStateRecord GetRoutineRecord(string routineId)
        {
            lock (Routines)
            {
                if (Routines.TryGetValue(routineId, out var routineRecord))
                    return routineRecord;
            }
            throw new InvalidOperationException($"Routine with ID '{routineId}' does not exist.");
        }

        private static int _idCounter;

        public static InMemoryDataStore Create(Action<Message> scheduleMessageAction)
        {
            var id = Interlocked.Increment(ref _idCounter);
            var dataStore = new InMemoryDataStore
            {
                Id = id,
                ScheduleMessage = scheduleMessageAction
            };
            lock (_dataStoreMap)
            {
                _dataStoreMap.Add(id, dataStore);
            }
            return dataStore;
        }

        public static bool TryGet(int id, out InMemoryDataStore dataStore)
        {
            lock (_dataStoreMap)
            {
                return _dataStoreMap.TryGetValue(id, out dataStore);
            }
        }
    }
}
