using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using FluentAssertions;
using Loom.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Loom.Messaging.Azure;

[TestClass]
public class EventProcessor_specs
{
    [TestMethod, AutoData]
    public async Task sut_processes_event_correctly(
        IEventConverter converter,
        LoggingMessageHandler spy,
        string id,
        string processId,
        string initiator,
        string predecessorId,
        MessageData1 data)
    {
        EventProcessor sut = new EventProcessorBuilder(converter, spy).Build();
        Message message = new(id, processId, initiator, predecessorId, data);
        EventData eventData = converter.ConvertToEvent(message);

        await sut.Process(new[] { eventData });

        spy.Log.Should().ContainSingle();
        spy.Log.Single().Should().BeEquivalentTo(message);
    }

    [TestMethod, AutoData]
    public async Task given_unknown_type_then_sut_ignores_message(
        IEventConverter converter,
        LoggingMessageHandler spy,
        Message message,
        string unknownType)
    {
        EventProcessor sut = new EventProcessorBuilder(converter, spy).Build();
        EventData eventData = converter.ConvertToEvent(message);
        eventData.Properties["Type"] = unknownType;

        await sut.Process(new[] { eventData });

        spy.Log.Should().BeEmpty();
    }

    [TestMethod, AutoData]
    public async Task given_unhandlable_message_then_sut_ignores_it(
        IEventConverter converter,
        IMessageHandler handler,
        Message message)
    {
        Mock.Get(handler).Setup(x => x.CanHandle(It.IsAny<Message>())).Returns(false);
        EventProcessor sut = new EventProcessorBuilder(converter, handler).Build();
        EventData eventData = converter.ConvertToEvent(message);

        await sut.Process(new[] { eventData });

        Mock.Get(handler).Verify(
            x => x.Handle(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [TestMethod, AutoData]
    public void sut_throws_aggregate_exception_for_bad_messages(
        IEventConverter converter,
        IMessageHandler handler,
        (Message Message, Exception Exception)[] tuples)
    {
        // Arrange
        EventProcessor sut = new EventProcessorBuilder(converter, handler).Build();

        var mock = Mock.Get(handler);
        foreach ((Message message, Exception exception) in tuples)
        {
            mock.Setup(x => x.CanHandle(It.Is<Message>(m => m.Id == message.Id))).Returns(true);

            Expression<Func<IMessageHandler, Task>> call = x => x.Handle(
                It.Is<Message>(m => m.Id == message.Id),
                It.IsAny<CancellationToken>());
            mock.Setup(call).ThrowsAsync(exception);
        }

        IEnumerable<EventData> events = tuples
            .Select(t => t.Message)
            .Select(converter.ConvertToEvent);

        // Act
        Func<Task> action = () => sut.Process(events);

        // Assert
        action.Should().ThrowAsync<AggregateException>()
              .GetAwaiter().GetResult()
              .Which.InnerExceptions
              .Should().BeEquivalentTo(tuples.Select(t => t.Exception));
    }

    [TestMethod]
    [InlineAutoData("ko-KR")]
    [InlineAutoData("en-US")]
    [InlineAutoData("ja-JP")]
    public async Task sut_sets_current_culture_with_locale(
        string locale,
        IEventConverter converter,
        CultureSnapshotter cultureSnapshotter,
        Message message)
    {
        // Arrange
        EventProcessorBuilder builder = new(converter, cultureSnapshotter);
        EventProcessor sut = builder.Build();

        EventData eventData = converter.ConvertToEvent(message);
        eventData.Properties["Locale"] = locale;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        // Act
        await sut.Process(new[] { eventData });

        // Assert
        cultureSnapshotter.Culture.Name.Should().Be(locale);
    }

    [TestMethod]
    [InlineAutoData("ko-KR")]
    [InlineAutoData("en-US")]
    [InlineAutoData("ja-JP")]
    public async Task sut_sets_current_ui_culture_with_locale(
        string locale,
        IEventConverter converter,
        CultureSnapshotter cultureSnapshotter,
        Message message)
    {
        // Arrange
        EventProcessorBuilder builder = new(converter, cultureSnapshotter);
        EventProcessor sut = builder.Build();

        EventData eventData = converter.ConvertToEvent(message);
        eventData.Properties["Locale"] = locale;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        // Act
        await sut.Process(new[] { eventData });

        // Assert
        cultureSnapshotter.UICulture.Name.Should().Be(locale);
    }

    [TestMethod]
    [InlineAutoData(new object?[] { null })]
    [InlineAutoData("")]
    [InlineAutoData(" ")]
    [InlineAutoData("\t")]
    [InlineAutoData("\r")]
    [InlineAutoData("\n")]
    [InlineAutoData(" \t\r\n")]
    public async Task sut_sets_current_culture_to_invariant_if_locale_is_empty(
        string? locale,
        IEventConverter converter,
        CultureSnapshotter cultureSnapshotter,
        Message message)
    {
        // Arrange
        EventProcessorBuilder builder = new(converter, cultureSnapshotter);
        EventProcessor sut = builder.Build();

        EventData eventData = converter.ConvertToEvent(message);
        eventData.Properties["Locale"] = locale;

        CultureInfo.CurrentCulture = CultureInfo.InstalledUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;

        // Act
        await sut.Process(new[] { eventData });

        // Assert
        CultureInfo actual = cultureSnapshotter.Culture;
        actual.Should().BeSameAs(CultureInfo.InvariantCulture);
    }

    [TestMethod, AutoData]
    public async Task sut_sets_current_culture_to_invariant_if_locale_is_non_string(
        IEventConverter converter,
        CultureSnapshotter cultureSnapshotter,
        Message message)
    {
        // Arrange
        EventProcessorBuilder builder = new(converter, cultureSnapshotter);
        EventProcessor sut = builder.Build();

        EventData eventData = converter.ConvertToEvent(message);
        eventData.Properties["Locale"] = Guid.NewGuid();

        CultureInfo.CurrentCulture = CultureInfo.InstalledUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;

        // Act
        await sut.Process(new[] { eventData });

        // Assert
        CultureInfo actual = cultureSnapshotter.Culture;
        actual.Should().BeSameAs(CultureInfo.InvariantCulture);
    }

    public class CultureSnapshotter : IMessageHandler
    {
        public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

        public CultureInfo UICulture { get; set; } = CultureInfo.InvariantCulture;

        public bool CanHandle(Message message) => true;

        public Task Handle(Message message, CancellationToken cancellationToken)
        {
            Culture = CultureInfo.CurrentCulture;
            UICulture = CultureInfo.CurrentUICulture;
            return Task.CompletedTask;
        }
    }
}
