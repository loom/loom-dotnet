namespace Loom.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using FluentAssertions;
    using Loom.Json;
    using Loom.Testing;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            MessageData1 data,
            TracingProperties tracingProperties,
            [Frozen] IJsonProcessor jsonProcessor,
            EventConverter sut)
        {
            var message = new Message(id, data, tracingProperties);

            EventData actual = sut.ConvertToEvent(message);

            string body = Encoding.UTF8.GetString(actual.Body.Array);
            MessageData1 content = jsonProcessor.FromJson<MessageData1>(body);
            content.Should().BeEquivalentTo(data);
        }

        [TestMethod, AutoData]
        public void ConvertToEvent_sets_properties_correctly(
            string id,
            MessageData1 data,
            TracingProperties tracingProperties,
            [Frozen] TypeResolver typeResolver,
            EventConverter sut)
        {
            var message = new Message(id, data, tracingProperties);

            EventData actual = sut.ConvertToEvent(message);

            IDictionary<string, object> properties = actual.Properties;
            properties.Should().Contain("Id", id);
            properties.Should().Contain("Type", typeResolver.ResolveTypeName<MessageData1>());
            properties.Should().Contain("OperationId", tracingProperties.OperationId);
            properties.Should().Contain("Contributor", tracingProperties.Contributor);
            properties.Should().Contain("ParentId", tracingProperties.ParentId);
        }

        [TestMethod, AutoData]
        public void TryConvertToMessage_deserializes_data_correctly(
            string id,
            MessageData1 data,
            TracingProperties tracingProperties,
            EventConverter sut)
        {
            var message = new Message(id, data, tracingProperties);
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
            actual.TracingProperties.Should().BeEquivalentTo(message.TracingProperties);
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
            eventData.Properties.Remove("Id");

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Id.Should().NotBeNull();
            Guid.TryParse(actual.Id, out Guid id).Should().BeTrue();
            id.Should().NotBeEmpty();
        }

        [TestMethod]
        [InlineAutoData(true)]
        [InlineAutoData(1)]
        public void given_non_string_id_property_then_TryConvertToMessage_sets_id_arbitrarily(
            object value, Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties["Id"] = value;

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Id.Should().NotBeNull();
            Guid.TryParse(actual.Id, out Guid id).Should().BeTrue();
            id.Should().NotBeEmpty();
        }

        [TestMethod, AutoData]
        public void given_no_operation_id_property_then_TryConvertToMessage_sets_operation_id_arbitrarily(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties.Remove("OperationId");

            Message actual = sut.TryConvertToMessage(eventData);

            actual.TracingProperties.OperationId.Should().NotBeNull();
            Guid.TryParse(actual.TracingProperties.OperationId, out Guid operationId).Should().BeTrue();
            operationId.Should().NotBeEmpty();
        }

        [TestMethod]
        [InlineAutoData(true)]
        [InlineAutoData(1)]
        public void given_non_string_operation_id_property_then_TryConvertToMessage_sets_operation_id_arbitrarily(
            object value, Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties["OperationId"] = value;

            Message actual = sut.TryConvertToMessage(eventData);

            actual.TracingProperties.OperationId.Should().NotBeNull();
            Guid.TryParse(actual.TracingProperties.OperationId, out Guid operationId).Should().BeTrue();
            operationId.Should().NotBeEmpty();
        }

        [TestMethod, AutoData]
        public void given_no_contributor_property_then_TryConvertToMessage_sets_contributor_to_null(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties.Remove("Contributor");

            Message actual = sut.TryConvertToMessage(eventData);

            actual.TracingProperties.Contributor.Should().BeNull();
        }

        [TestMethod, AutoData]
        public void given_no_parent_id_property_then_TryConvertToMessage_sets_parent_id_to_null(
            Message message, EventConverter sut)
        {
            EventData eventData = sut.ConvertToEvent(message);
            eventData.Properties.Remove("ParentId");

            Message actual = sut.TryConvertToMessage(eventData);

            actual.TracingProperties.ParentId.Should().BeNull();
        }

        [TestMethod, AutoData]
        public void TryConvertToMessage_returns_null_if_body_array_is_null(
            [Frozen] TypeResolver typeResolver,
            Message message,
            EventConverter sut)
        {
            var eventData = new EventData(default(ArraySegment<byte>));
            eventData.Properties["Type"] = typeResolver.ResolveTypeName(message.Data.GetType());

            Message actual = sut.TryConvertToMessage(eventData);

            actual.Should().BeNull();
        }
    }
}
