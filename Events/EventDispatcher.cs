using AutoFollow.Networking;
using AutoFollow.Resources;

namespace AutoFollow.Events
{
    /// <summary>
    /// Stores all the information needed to fire an event at a later time.
    /// </summary>
    public class EventDispatcher
    {
        public EventDispatcher(AsyncEvent<Message, EventData> e, EventData eventData, Message senderMessage)
        {
            AsyncEvent = e;
            EventData = eventData;
            SenderMessage = senderMessage;
        }

        public AsyncEvent<Message, EventData> AsyncEvent { get; private set; }
        public EventData EventData { get; private set; }
        public Message SenderMessage { get; private set; }
    }
}

