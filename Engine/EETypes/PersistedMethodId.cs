namespace Dasync.EETypes
{
    public class PersistedMethodId : MethodId
    {
        /// <summary>
        /// An ID of the intent that triggered execution of this routine.
        /// Used for correlation purposes.
        /// </summary>
        public string IntentId { get; set; }

        public string ETag { get; set; }

        public override MethodId Clone() => CopyTo(new PersistedMethodId());

        public override T CopyTo<T>(T copy)
        {
            base.CopyTo(copy);
            var x = (PersistedMethodId)(object)copy;
            x.IntentId = IntentId;
            x.ETag = ETag;
            return copy;
        }
    }
}
