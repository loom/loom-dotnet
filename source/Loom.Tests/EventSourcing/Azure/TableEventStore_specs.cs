namespace Loom.EventSourcing.Azure
{
    using Loom.Messaging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TableEventStore_specs : EventStoreUnitTests<TableEventStore>
    {
        protected override TableEventStore GenerateEventStore(
            TypeResolver typeResolver, IMessageBus eventBus)
        {
            return new TableEventStore(StorageEmulator.EventStoreTable, typeResolver, eventBus);
        }
    }
}
