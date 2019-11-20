using System;

namespace Dasync.EETypes.Persistence
{
    public class MethodStateStorageNotFoundException : Exception
    {
        public MethodStateStorageNotFoundException() : base() { }

        public MethodStateStorageNotFoundException(string message) : base(message) { }
    }
}
