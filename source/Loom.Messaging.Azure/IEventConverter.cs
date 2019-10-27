namespace Loom.Messaging.Azure
{
    using Microsoft.Azure.EventHubs;

    public interface IEventConverter
    {
        EventData ConvertToEvent(Message message);

        Message? TryConvertToMessage(EventData eventData);
    }
}
