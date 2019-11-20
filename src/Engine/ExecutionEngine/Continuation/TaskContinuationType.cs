namespace Dasync.ExecutionEngine.Continuation
{
    public enum TaskContinuationType
    {
        /// <summary>
        /// Not recognized continuation type
        /// </summary>
        Unknown,

        /// <summary>
        /// Dummy continuation object - no op
        /// </summary>
        None,

        /// <summary>
        /// A collection of continuation objects (List&lt;object&gt;)
        /// </summary>
        ContinuationList,

        /// <summary>
        /// A continuation is the IAsyncStateMachine.MoveNext()
        /// </summary>
        AsyncStateMachine,

        ///// <summary>
        ///// A delegate wrapped in a Task - created when Task.ContinueWith is called
        ///// </summary>
        Standard,

        /// <summary>
        /// Continuation is an object that tracks completion of multiple tasks when Task.WhenAll is called.
        /// </summary>
        WhenAll
    }
}
