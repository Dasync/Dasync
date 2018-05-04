using Microsoft.WindowsAzure.Storage.Table;

namespace Dasync.FabricConnector.AzureStorage
{
    public class RoutineRecord : TableEntity
    {
        /// <summary>
        /// Name of the method (solely for information purposes).
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// See <see cref="RoutineStatus"/>.
        /// </summary>
        public int Status { get; set; }

        //public string Input { get; set; }

        public string State { get; set; }

        public string Result { get; set; }

        public string Continuation { get; set; }

        /// <summary>
        /// Name of the service that triggered this routine (solely for information purposes).
        /// </summary>
        public string CallerService { get; set; }

        /// <summary>
        /// Name of the method that represents routine of the <see cref="CallerService"/>
        /// (solely for information purposes).
        /// </summary>
        public string CallerMethod { get; set; }

        /// <summary>
        /// The ID of the routine of the <see cref="CallerMethod"/>
        /// (solely for information purposes).
        /// </summary>
        public string CallerRoutineId { get; set; }

        /// <summary>
        /// (solely for information purposes).
        /// </summary>
        public string AwaitService { get; set; }

        /// <summary>
        /// (solely for information purposes).
        /// </summary>
        public string AwaitMethod { get; set; }

        /// <summary>
        /// (solely for information purposes).
        /// </summary>
        public long? AwaitIntentId { get; set; }
    }
}
