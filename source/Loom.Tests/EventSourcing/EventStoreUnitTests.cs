using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Loom.Json;
using Loom.Messaging;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Loom.EventSourcing
{
    public abstract class EventStoreUnitTests<T>
        where T : IEventCollector, IEventReader
    {
        protected static TypeResolver TypeResolver { get; } = new TypeResolver(
            new FullNameTypeNameResolvingStrategy(),
            new TypeResolvingStrategy());

        protected static IJsonProcessor JsonProcessor { get; } = new JsonProcessor(new JsonSerializer());

        protected abstract T GenerateEventStore(
            TypeResolver typeResolver,
            IMessageBus eventBus);

        protected T GenerateEventStore(IMessageBus eventBus)
            => GenerateEventStore(TypeResolver, eventBus);

        [TestMethod, AutoData]
        public async Task QueryEvents_restores_events_correctly(
            IMessageBus eventBus,
            string streamId,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            Event4 evt4)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus);
            var events = new List<object>(
                from e in new object[] { evt1, evt2, evt3, evt4 }
                orderby e.GetHashCode()
                select e);

            await sut.CollectEvents(streamId, startVersion: 1, events);

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events, c => c.WithStrictOrdering());
        }

        [TestMethod, AutoData]
        public async Task QueryEvents_filters_events_by_stream_id(
            IMessageBus eventBus,
            string streamId,
            string otherStreamId,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            Event4 evt4)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus);
            int startVersion = 1;
            var events = new List<object>(
                from e in new object[] { evt1, evt2, evt3, evt4 }
                orderby e.GetHashCode()
                select e);

            await sut.CollectEvents(streamId, startVersion, events);
            await sut.CollectEvents(otherStreamId, startVersion, events);

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 1);

            // Assert
            actual.Should().BeEquivalentTo(events, c => c.WithStrictOrdering());
        }

        [TestMethod, AutoData]
        public async Task QueryEvents_filters_events_by_version(
            IMessageBus eventBus,
            string streamId,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            Event4 evt4)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus);
            var events = new List<object>(
                from e in new object[] { evt1, evt2, evt3, evt4 }
                orderby e.GetHashCode()
                select e);

            await sut.CollectEvents(streamId, startVersion: 1, events);

            // Act
            IEnumerable<object> actual = await sut.QueryEvents(streamId, fromVersion: 2);

            // Assert
            actual.Should().BeEquivalentTo(events.Skip(1), c => c.WithStrictOrdering());
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_controls_concurrency(
            IMessageBus eventBus, string streamId, Event4 evt)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus);
            int version = 1;
            object[] events = new[] { evt };
            await sut.CollectEvents(streamId, startVersion: version, events);

            // Act
            Func<Task> action = () => sut.CollectEvents(streamId, startVersion: version, events);

            // Assert
            await action.Should().ThrowAsync<Exception>();
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_throws_if_cannot_resolve_event_type_name(
            TypeResolvingStrategy typeStrategy,
            FullNameTypeNameResolvingStrategy typeNameStrategy,
            IMessageBus eventBus,
            string streamId,
            Event1 evt)
        {
            // Arrange
            var typeResolver = new TypeResolver(
                Mock.Of<ITypeNameResolvingStrategy>(
                    x =>
                    x.TryResolveTypeName(typeof(Event1)) == null &&
                    x.TryResolveTypeName(typeof(State1)) == typeNameStrategy.TryResolveTypeName(typeof(State1))),
                typeStrategy);

            T sut = GenerateEventStore(typeResolver, eventBus);

            // Act
            Func<Task> action = () => sut.CollectEvents(streamId, startVersion: 1, new[] { evt });

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_sends_messages_correctly(
            string streamId,
            MessageBusDouble spy,
            int startVersion,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            Event4 evt4,
            string processId,
            string initiator,
            string predecessorId)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus: spy);
            object[] events = new object[] { evt1, evt2, evt3, evt4 };

            // Act
            await sut.CollectEvents(processId,
                                    initiator,
                                    predecessorId,
                                    streamId,
                                    startVersion,
                                    events,
                                    cancellationToken: default);

            // Assert
            spy.Calls.Should().ContainSingle();

            (ImmutableArray<Message> messages, string partitionKey) = spy.Calls.Single();

            messages.Should()
                .HaveCount(events.Length)
                .And.OnlyContain(
                    x =>
                    x.ProcessId == processId &&
                    x.Initiator == initiator &&
                    x.PredecessorId == predecessorId);

            VerifyData(messages[0].Data, startVersion + 0, evt1);
            VerifyData(messages[1].Data, startVersion + 1, evt2);
            VerifyData(messages[2].Data, startVersion + 2, evt3);
            VerifyData(messages[3].Data, startVersion + 3, evt4);

            void VerifyData<TPayload>(object source,
                                      long expectedVersion,
                                      TPayload expectedPayload)
            {
                source.Should().BeOfType<StreamEvent<TPayload>>();
                var data = (StreamEvent<TPayload>)source;
                data.StreamId.Should().Be(streamId);
                data.Version.Should().Be(expectedVersion);
                data.Payload.Should().BeEquivalentTo(expectedPayload);
            }

            partitionKey.Should().Be($"{streamId}");
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_does_not_send_messages_if_it_failed_to_save_events(
            MessageBusDouble spy, string streamId, int version, Event1 evt1, Event2 evt2)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus: spy);
            await sut.CollectEvents(streamId, startVersion: version, new[] { evt1 });
            spy.Clear();

            // Act
            Func<Task> action = () => sut.CollectEvents(streamId, version, new[] { evt2 });

            // Assert
            await action.Should().ThrowAsync<Exception>();
            spy.Calls.Should().BeEmpty();
        }

        [TestMethod, AutoData]
        public async Task if_CollectEvents_failed_to_send_messages_it_sends_them_next_time(
            string streamId,
            int startVersion,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            Event4 evt4)
        {
            // Arrange
            MessageBusDouble spy = new(errors: 1);
            T sut = GenerateEventStore(eventBus: spy);

            // Act
            try
            {
                await sut.CollectEvents(streamId, startVersion, new object[] { evt1, evt2 });
            }
            catch
            {
            }

            await sut.CollectEvents(streamId, startVersion + 2, new object[] { evt3, evt4 });

            // Assert
            (ImmutableArray<Message> Messages, string PartitionKey)[] calls = spy.Calls.ToArray();

            calls.Should().HaveCount(3);

            calls[1].Messages.Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event1>(streamId, startVersion + 0, default, evt1),
                new StreamEvent<Event2>(streamId, startVersion + 1, default, evt2),
            },
            c => c.WithStrictOrdering().Excluding((IMemberInfo m) => m.Name == "RaisedTimeUtc"));
            calls[1].PartitionKey.Should().Be($"{streamId}");

            calls[2].Messages.Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event3>(streamId, startVersion + 2, default, evt3),
                new StreamEvent<Event4>(streamId, startVersion + 3, default, evt4),
            },
            c => c.WithStrictOrdering().Excluding((IMemberInfo m) => m.Name == "RaisedTimeUtc"));
            calls[2].PartitionKey.Should().Be($"{streamId}");
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_does_not_send_messages_again(
            MessageBusDouble spy,
            string streamId,
            int startVersion,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            Event4 evt4)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus: spy);

            // Act
            await sut.CollectEvents(streamId, startVersion, new object[] { evt1, evt2 });
            await sut.CollectEvents(streamId, startVersion + 2, new object[] { evt3, evt4 });

            // Assert
            (ImmutableArray<Message> Messages, string PartitionKey)[] calls = spy.Calls.ToArray();

            calls.Should().HaveCount(2);

            calls[0].Messages.Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event1>(streamId, startVersion + 0, default, evt1),
                new StreamEvent<Event2>(streamId, startVersion + 1, default, evt2),
            },
            c => c.WithStrictOrdering().Excluding((IMemberInfo m) => m.Name == "RaisedTimeUtc"));
            calls[0].PartitionKey.Should().Be($"{streamId}");

            calls[1].Messages.Select(x => x.Data).Should().BeEquivalentTo(new object[]
            {
                new StreamEvent<Event3>(streamId, startVersion + 2, default, evt3),
                new StreamEvent<Event4>(streamId, startVersion + 3, default, evt4),
            },
            c => c.WithStrictOrdering().Excluding((IMemberInfo m) => m.Name == "RaisedTimeUtc"));
            calls[1].PartitionKey.Should().Be($"{streamId}");
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_sets_message_id_properties_to_unique_values(
            MessageBusDouble spy,
            string streamId,
            int startVersion,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            Event4 evt4)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus: spy);

            // Act
            await sut.CollectEvents(streamId, startVersion, new object[] { evt1, evt2 });
            await sut.CollectEvents(streamId, startVersion + 2, new object[] { evt3, evt4 });

            // Assert
            spy.Calls.SelectMany(x => x.Messages).Select(x => x.Id).Should().OnlyHaveUniqueItems();
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_preserves_message_id(
            string streamId,
            int startVersion,
            Event1 evt1,
            Event2 evt2)
        {
            // Arrange
            MessageBusDouble spy = new(errors: 1);
            T sut = GenerateEventStore(eventBus: spy);

            // Act
            try
            {
                await sut.CollectEvents(streamId, startVersion, new[] { evt1 });
            }
            catch
            {
            }

            await sut.CollectEvents(streamId, startVersion + 1, new[] { evt2 });

            // Assert
            spy.Calls.SelectMany(x => x.Messages)
                     .Select(x => x.Id)
                     .Take(2)
                     .Distinct()
                     .Should()
                     .ContainSingle();
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_sets_RaisedTimeUtc_property_correctly(
            MessageBusDouble spy, string streamId, Event1 evt)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus: spy);
            int startVersion = 1;
            DateTime nowUtc = DateTime.UtcNow;

            // Act
            await sut.CollectEvents(streamId, startVersion, new[] { evt });

            // Assert
            Message message = spy.Calls.SelectMany(x => x.Messages).Single();
            DateTime actual = message.Data.As<StreamEvent<Event1>>().RaisedTimeUtc;
            actual.Kind.Should().Be(DateTimeKind.Utc);
            actual.Should().BeCloseTo(nowUtc, precision: TimeSpan.FromSeconds(1));
        }

        [TestMethod, AutoData]
        public async Task CollectEvents_preserves_RaisedTimeUtc_property(
            string streamId,
            int startVersion,
            Event1 evt1,
            Event2 evt2)
        {
            // Arrange
            MessageBusDouble spy = new(errors: 1);
            T sut = GenerateEventStore(eventBus: spy);

            // Act
            try
            {
                await sut.CollectEvents(streamId, startVersion, new[] { evt1 });
            }
            catch
            {
            }

            await Task.Delay(millisecondsDelay: 100);
            await sut.CollectEvents(streamId, startVersion + 1, new[] { evt2 });

            // Assert
            spy.Calls.SelectMany(x => x.Messages)
                     .Select(x => x.Data)
                     .Cast<StreamEvent<Event1>>()
                     .Select(x => x.RaisedTimeUtc)
                     .Take(2)
                     .Distinct()
                     .Should()
                     .ContainSingle();
        }

        [TestMethod, AutoData]
        public async Task QueryEventMessages_correctly_restores_event_messages(
            string streamId,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            IMessageBus eventBus,
            string processId,
            string initiator,
            string predecessorId)
        {
            // Arrange
            T sut = GenerateEventStore(eventBus);
            object[] events = new object[] { evt1, evt2, evt3 };
            DateTime nowUtc = DateTime.UtcNow;
            await sut.CollectEvents(processId,
                                    initiator,
                                    predecessorId,
                                    streamId,
                                    startVersion: 1,
                                    events,
                                    cancellationToken: default);

            // Act
            IEnumerable<Message> actual = await sut.QueryEventMessages(streamId);

            // Assert
            actual.Should().HaveSameCount(events);

            actual.Cast<dynamic>()
                  .Select(x => (string)x.Data.StreamId)
                  .Should().OnlyContain(x => x == streamId);

            actual.Cast<dynamic>()
                  .Select(x => (long)x.Data.Version)
                  .Should().BeEquivalentTo(new[] { 1, 2, 3 });

            actual.Cast<dynamic>()
                  .Select(x => (object)x.Data.Payload)
                  .Should().BeEquivalentTo(events);

            foreach (DateTime raisedTime in actual.Cast<dynamic>()
                                                  .Select(x => x.Data.RaisedTimeUtc))
            {
                raisedTime.Kind.Should().Be(DateTimeKind.Utc);
                raisedTime.Should().BeCloseTo(nowUtc, precision: TimeSpan.FromSeconds(1));
            }

            actual.ElementAt(0).Data.Should().BeOfType<StreamEvent<Event1>>();
            actual.ElementAt(1).Data.Should().BeOfType<StreamEvent<Event2>>();
            actual.ElementAt(2).Data.Should().BeOfType<StreamEvent<Event3>>();

            actual.Select(x => x.Id).Should().OnlyHaveUniqueItems();
            actual.Should().OnlyContain(
                x =>
                x.ProcessId == processId &&
                x.Initiator == initiator &&
                x.PredecessorId == predecessorId);
        }

        [TestMethod, AutoData]
        public async Task QueryEventMessages_returns_same_value_for_same_source(
            string streamId,
            Event1 evt1,
            Event2 evt2,
            Event3 evt3,
            IMessageBus eventBus)
        {
            T sut = GenerateEventStore(eventBus);
            object[] events = new object[] { evt1, evt2, evt3 };
            await sut.CollectEvents(streamId, startVersion: 1, events);

            IEnumerable<Message> actual = await sut.QueryEventMessages(streamId);

            actual.Should().BeEquivalentTo(await sut.QueryEventMessages(streamId));
        }

        [TestMethod, AutoData]
        public async Task QueryEvents_throws_if_cannot_resolve_event_type(
            FullNameTypeNameResolvingStrategy typeNameStrategy,
            string streamId,
            Event1 evt,
            IMessageBus eventBus)
        {
            // Arrange
            string typeName = typeNameStrategy.TryResolveTypeName(typeof(Event1));
            var typeResolver = new TypeResolver(
                typeNameStrategy,
                Mock.Of<ITypeResolvingStrategy>(x => x.TryResolveType(typeName) == null));

            T sut = GenerateEventStore(typeResolver, eventBus);
            object[] events = new object[] { evt };
            await sut.CollectEvents(streamId, startVersion: 1, events);

            // Act
            Func<Task> action = () => sut.QueryEvents(streamId);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod, AutoData]
        public async Task QueryEventMessages_throws_if_cannot_resolve_event_type(
            FullNameTypeNameResolvingStrategy typeNameStrategy,
            string streamId,
            Event1 evt,
            IMessageBus eventBus)
        {
            // Arrange
            string typeName = typeNameStrategy.TryResolveTypeName(typeof(Event1));
            var typeResolver = new TypeResolver(
                typeNameStrategy,
                Mock.Of<ITypeResolvingStrategy>(x => x.TryResolveType(typeName) == null));

            T sut = GenerateEventStore(typeResolver, eventBus);
            object[] events = new object[] { evt };
            await sut.CollectEvents(streamId, startVersion: 1, events);

            // Act
            Func<Task> action = () => sut.QueryEventMessages(streamId);

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
