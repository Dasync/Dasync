﻿using System;
using System.Diagnostics;

namespace Dasync.EETypes
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class EventId
    {
        /// <summary>
        /// The name of the event which is extracted with reflection.
        /// </summary>
        public string Name { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get
            {
                return Name;
            }
        }

        public override bool Equals(object obj) =>
            (obj is EventId eventId)
            ? this == eventId
            : base.Equals(obj);

        public override int GetHashCode() =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? "");

        public static bool operator ==(EventId a, EventId b) => string.Equals(a?.Name, b?.Name);

        public static bool operator !=(EventId a, EventId b) => !(a == b);

        public void Deconstruct(out string name)
        {
            name = Name;
        }

        public EventId Clone() => CopyTo(new EventId());

        public T CopyTo<T>(T copy) where T : EventId
        {
            copy.Name = Name;
            return copy;
        }
    }
}
