using System;
using System.Collections.Generic;
using System.Text;
using Azure.Messaging.EventHubs;
using FluentAssertions;
using Loom.Json;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.Messaging.Azure
{
    [TestClass]
    public class EventConverter_specs
    {
        [TestMethod]
        public void sut_implements_IEventConverter()
        {
            typeof(EventConverter).Should().Implement<IEventConverter>();
        }

        [TestMethod, AutoData]
        public void ConvertToEvent_serializes_data_correctly(
            string id,
            string processId,
            string initiator,
            string predecessorId,
            MessageData1 data,
            [Frozen] IJsonProcessor jsonProcessor,
            EventConverter sut)
        {
            var message = new Message(id, processId, initiator, predecessorId, data);

            EventData actual = sut.ConvertToEvent(message);

            string body = Encoding.UTF8.GetString(actual.EventBody.ToArray());
            MessageData1 content = jsonProcessor.FromJson<MessageData1>(body);
            content.Should().BeEquivalentTo(data);
        }

        [TestMethod, AutoData]
        public void ConvertToEvent_sets_id_correctly(
            string id,
            string processId,
            string initiator,
            string predecessorId,
            MessageData1 data,
            EventConverter sut)
        {
            var message = new Message(id, processId, initiator, predecessorId, data);
            EventData actual = sut.ConvertToEvent(message);
            actual.MessageId.Should().Be(id);
        }

        [TestMethod, AutoData]
        public void ConvertToEvent_sets_properties_correctly(
            string id,
            string processId,
            string initiator,
            string predecessorId,
            MessageData1 data,
            [Frozen] TypeResolver typeResolver,
            EventConverter sut)
        {
            var message = new Message(id, processId, initiator, predecessorId, data);

            EventData actual = sut.ConvertToEvent(message);

            IDictionary<string, object> properties = actual.Properties;
            properties.Should().Contain("Type", typeResolver.TryResolveTypeName<MessageData1>());
            properties.Should().Contain("ProcessId", processId);
            properties.Should().Contain("Initiator", initiator);
            properties.Should().Contain("PredecessorId", predecessorId);
        }

        [TestMethod, AutoData]
        public void TryConvertToMessage_deserializes_data_correctly(
            string id,
            string processId,
            string initiator,
            string predecessorId,
            MessageData1 data,
            EventConverter sut)
        {
            var message = new Message(id, processId, initiator, predecessorId, data);
            EventData eventData = sut.ConvertToEvent(message);

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Data.Should().BeEquivalentTo(data);
        }

        [TestMethod, AutoData]
        public void TryConvertToMessage_sets_id_correctly(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            Message actual = sut.TryConvertToMessage(eventData);
            actual.Id.Should().Be(message.Id);
        }

        [TestMethod, AutoData]
        public void TryConvertToMessage_sets_tracing_properties_correctly(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);

            Message actual = sut.TryConvertToMessage(eventData);

            actual.ProcessId.Should().Be(message.ProcessId);
            actual.Initiator.Should().Be(message.Initiator);
            actual.PredecessorId.Should().Be(message.PredecessorId);
        }

        [TestMethod, AutoData]
        public void given_no_type_property_then_TryConvertToMessage_returns_null(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties.Remove("Type");

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Should().BeNull();
        }

        [TestMethod]
        [InlineAutoData(true)]
        [InlineAutoData(1)]
        public void given_non_string_type_property_then_TryConvertToMessage_returns_null(
            object value, Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties["Type"] = value;

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Should().BeNull();
        }

        [TestMethod, AutoData]
        public void given_unknown_type_then_TryConvertToMessage_returns_null(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties["Type"] = "UnknownType";

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Should().BeNull();
        }

        [TestMethod, AutoData]
        public void given_no_id_property_then_TryConvertToMessage_sets_id_arbitrarily(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.MessageId = null;

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Id.Should().NotBeNull();
            Guid.TryParse(actual.Id, out Guid id).Should().BeTrue();
            id.Should().NotBeEmpty();
        }

        [TestMethod, AutoData]
        public void given_no_process_id_property_then_TryConvertToMessage_sets_it_arbitrarily(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties.Remove("ProcessId");

            Message actual = sut.TryConvertToMessage(eventData);

            actual.ProcessId.Should().NotBeNull();
            Guid.TryParse(actual.ProcessId, out Guid processId).Should().BeTrue();
            processId.Should().NotBeEmpty();
        }

        [TestMethod, AutoData]
        public void given_non_string_process_id_property_then_TryConvertToMessage_sets_it_arbitrarily(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties.Remove("ProcessId");

            Message actual = sut.TryConvertToMessage(eventData);

            actual.ProcessId.Should().NotBeNull();
            Guid.TryParse(actual.ProcessId, out Guid processId).Should().BeTrue();
            processId.Should().NotBeEmpty();
        }

        [TestMethod, AutoData]
        public void given_no_initiator_property_then_TryConvertToMessage_sets_it_to_null(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties.Remove("Initiator");

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Initiator.Should().BeNull();
        }

        [TestMethod, AutoData]
        public void given_no_predecessor_id_property_then_TryConvertToMessage_sets_it_to_null(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties.Remove("PredecessorId");

            Message actual = sut.TryConvertToMessage(eventData);

            actual.PredecessorId.Should().BeNull();
        }

        [TestMethod, AutoData]
        public void TryConvertToMessage_returns_null_if_body_array_is_null(
            [Frozen] TypeResolver typeResolver,
            Message message,
            EventConverter sut)
        {
            var eventData = new EventData(default(ArraySegment<byte>));
            eventData.Properties["Type"] = typeResolver.TryResolveTypeName(message.Data.GetType());

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Should().BeNull();
        }
    }
}
