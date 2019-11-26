# D-ASYNC Core Concepts

## Service Communication Primitives
The basic building blocks of a multi-service application are queries, commands, and events. Regardless of the underlying technical choices, these primitives describe what services do and their logical dependencies.

__Queries__ provide a snapshot of data at a certain point in time and should not modify any internal state of an application. Usually, queries are executed synchronously with HTTP GET or with an RPC-like technology. An example would be a query that tells the balance of your account in a bank.

__Commands__ convey an intent to perform an operation that modifies the internal state of an application. Similarly to queries, commands can be executed synchronously, but also can be invoked asynchronously. With the latter approach, the execution result can either be polled (active) or delivered to the caller on completion (passive). Commands use HTTP POST or PUT, a message queue, or an RPC-like technology. There is no guarantee that a command succeeds, the execution may fail - for example, you may not be able to withdraw from your bank account more than you have.

__Events__ represent something that already happened and cannot be changed. There may be a service command that can undo a certain change, but that does not erase any event. E.g. you may receive a text message notification that you just paid for goods or services with your credit card. Services may react to events to run a business logic required by the specification, where the event publisher does not wait for the subscribers to complete any processing. Events may use Kafka, RabbitMQ Exchanges, or even Redis Pub/Sub.

## Service Workflows

The code usually consists of two logical parts - business logic and non-functional. The business logic represents implemented requirements that bring value to end-users, where the non-functional code is concrete implementation details that work with specific interchangeable technology.

The business logic often has a series of steps that are missing critical and must execute in a reliable and fault-tolerant way. A single method/function can be invoked as a command or an event handler, which can invoke more commands and publish more events, thus creating a chain of calls and reactions to the results. Thus a method/function is a __routine__ of a __workflow__ that can consist of many sub-routines, where the intermediate state usually has to be saved to guarantee the execution.

The boundary of a workflow is not limited by one service; in fact, it is more often to see multiple services achieve a final result using orchestration and/or choreography model(s).

## D-ASYNC Model
D-ASYNC (short for **D**istributed **Async**hronous Functions) combines  communication primitives and workflows using new paradigms of a programming language itself, instead of using a framework, writing and maintaining the non-functional code.

The higher-level abstractions allow developers to focus on business logic, increase readability and maintainability of the code, reduce the code size, and cut the development time. The underlying implementation details tailored to a concrete set of technologies can be effortlessly changed with configuration to satisfy emerging application requirements and scalability targets.

----
### Read Next
- [Demo Application](../examples/AspNetCoreDocker/README.md)
- Extensibility
