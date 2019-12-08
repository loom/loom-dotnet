# LOOM.NET

LOOM.NET is a lightweight toolkit for implementing event-driven architecture on .NET Standard and .NET Core.

## Messaging

LOOM.NET provides the asynchronous one-way messaging abstraction and some implementations

```csharp
public interface IMessageBus
{
    Task Send(IEnumerable<Message> messages, string partitionKey);
}
```

```csharp
public interface IMessageHandler
{
    bool CanHandle(Message message);

    Task Handle(Message message);
}
```

## EventSourcing

LOOM.NET helps to implement the event sourcing pattern. LOOM.NET supports the following features:

- Event producing and event handling in a functional way
- Rolling snapshot
- Reliable event publishing
- Unique property indexing
