
public class ServerMessageEvent : GameEvent
{
    public const string ServerMessageEventTag = "ServerMessageEvent";
    public MessageXieYi ServerMessage;

    public ServerMessageEvent(MessageXieYi message) : base(ServerMessageEventTag)
    {
        ServerMessage = message;
    }
}
