using System;

namespace Dasync.EETypes.Intents
{
#warning Need to finalize the factory pattern implementation first.
    public sealed class CreateServiceInstanceIntent
    {
        //public Type ServiceType;

#warning Review factory pattern - intercept before of after routine execution? Before makes more sense - similar to the 'new' operator and memory allocation.
        //public object ServiceInstance;

#warning This is a temporary variable to avoid serialization/correlation problem between a subroutine result task (the factory) and transactionality of a caller's transition
        //public WorkflowStubInfo StubInfo;
    }
}
