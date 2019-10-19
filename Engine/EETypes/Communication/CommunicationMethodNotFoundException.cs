using System;

namespace Dasync.EETypes.Communication
{
    public class CommunicationMethodNotFoundException : Exception
    {
        public CommunicationMethodNotFoundException() : base() { }

        public CommunicationMethodNotFoundException(string message) : base(message) { }
    }
}
