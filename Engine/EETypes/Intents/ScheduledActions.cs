using System;
using System.Collections.Generic;

namespace Dasync.EETypes.Intents
{
    /// <summary>
    /// Contains a list of intended actions that need to performed in a
    /// transactional manner during a single transition of a state machine.
    /// </summary>
    public sealed class ScheduledActions
    {
        /// <summary>
        /// Describes all sub-routines and routines from other services that
        /// need to be invoked. That does not mean that current routine is
        /// awaiting on any or all of them.
        /// </summary>
        public List<ExecuteRoutineIntent> ExecuteRoutineIntents;

        /// <summary>
        /// Describes all events that need to be published.
        /// </summary>
        public List<RaiseEventIntent> RaiseEventIntents;

        /// <summary>
        /// Save the state of the current routine if it's a state machine.
        /// </summary>
#warning SaveRoutineState is just a flag indicating that SaveStateIntent must be initialized later
        public bool SaveRoutineState;

        public SaveStateIntent SaveStateIntent;

        public ContinueRoutineIntent ResumeRoutineIntent;

        /// <summary>
        /// A collection of continuations to invoke. In most cases it will be only 1 or none.
        /// </summary>
        public List<ContinueRoutineIntent> ContinuationIntents;

//        /// <summary>
//        /// Describes all service instances that need to be created.
//        /// </summary>
//#warning Need to finalize the factory concept first.
//        public List<CreateServiceInstanceIntent> CreateServiceIntents;

//        /// <summary>
//        /// Delete an instance of current service because current routine
//        /// being invoked is <see cref="IDisposable.Dispose"/>.
//        /// The pre-requirement for this operation is the instance of the
//        /// service must be created with factory pattern in first place.
//        /// See related <see cref="CreateServiceIntents"/>.
//        /// </summary>
//#warning Need to finalize the factory concept first.
//        public bool DeleteServiceInstance;

        public List<RegisterTriggerIntent> RegisterTriggerIntents;

        public List<SubscribeToTriggerIntent> SubscribeToTriggerIntents;

        public List<ActivateTriggerIntent> ActivateTriggerIntents;
    }
}
