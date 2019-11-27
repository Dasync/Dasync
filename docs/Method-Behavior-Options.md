# D-ASYNC Method Behavior Options
Behavior options can be set or overriden at any level in the [configuration](Configuration-Structure.md).

Here are the defaults setting values as if they were described in the configuration:
```json
{
    "dasync": {
        "queries": {
            "deduplicate": false,
            "resilient": false,
            "persistent": false,
            "roamingState": false,
            "transactional": false,
            "runInPlace": true,
            "ignoreTransaction": true
        },
        "commands": {
            "deduplicate": true,
            "resilient": true,
            "persistent": true,
            "roamingState": false,
            "transactional": true,
            "runInPlace": false,
            "ignoreTransaction": false
        },
        "events": {
            "deduplicate": false,
            "resilient": false,
            "ignoreTransaction": false
        }
    }
}
```

### **Deduplicate**
Deduplicate incoming messages using their ID or internal intent ID.

### **Resilient**
Prefer a method to be executed via a communication mechanism that has a message delivery guarantee. For example, an HTTP invocation can delegate execution to a message queue. Resiliency cannot be guaranteed if the desired communication mechanism does not support it.

### **Persistent**
 Method state should be saved and restored when sending commands (sometimes queries). If `false`, then wait for the command completion in process. Persistence won't be available if there are no such a meachnisms registered in the app, unless the `roamingState` is set to `true`.

### **RoamingState**
When `persistent` is enabled, convey the state of a method inside a command so the state is restored from the response instead of a persistence mechanism. Does not work with `Task.WhenAll` because it requires a concurrency check. Has no effect if a command (sometimes queries) has no continuation. The benefit of this setting is in performance when no persistent storage is used. However, many communication mechanisms have a message size limit which can be easily exceeded by adding the execution state to it.

### **Transactional**
Enables Unit of Work - send all commands/events and save DB entities at once when a method state transition completes. DB integration is not guaranteed. When `false`, any command or event (sometimes queries) will be sent immediately without any wait for the transition to complete. There are multiple implementation options like the outbox pattern or caching and message completion table.

### **RunInPlace**
Prefer to run a query or a command in the same process instead of scheduling a message. Queries of local services are invoked in place by default. If a command has the `persistent` option set, then the invocation in place is only possible when the communication method supports a message lock on publishing. You can think of this behavior as 'high priority' (cut in front of other messages on the queue) and 'low latency' (no need to wait till the message is picked up, processed, and the result is polled back).

### **IgnoreTransaction**
Ignore the transaction even if the method that calls this command (sometimes a query) is transctional. This allows better latency but no consistency. Queries ignore transactions by default.