namespace Dasync.Serialization
{
    public struct ValueInfo
    {
        public string Name;
        public int? Index;
        public TypeSerializationInfo Type;
        public long? ReferenceId;
        public string SpecialId;
        public bool IsCollection;
        public int? ItemCount;
#warning ItemType is not needed - must be deterministic from the collection type
        public TypeSerializationInfo ItemType;
    }
}
