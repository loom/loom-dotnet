using Azure.Messaging.EventHubs;

namespace Loom.Messaging.Azure
{
    public interface IEventConverter
    {
        EventData ConvertToEvent(Message message);

        Message? TryConvertToMessage(EventData eventData);
    }
}
