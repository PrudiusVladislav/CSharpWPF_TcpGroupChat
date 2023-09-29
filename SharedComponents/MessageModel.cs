namespace SharedComponents;

public class MessageModel
{
    public const string ExitMessage = "EXIT";
    public const string ExitMessageResponse = "EXIT_RESPONSE";
    public const string ClientDisconnectMessage = "You have been disconnected";
    
    //message option is the byte message send before main message that identify what kind of a message it is:
    // 0 - common text message
    // 1 - some system message like EXIT or message notifying clients that a new client has been added etc.
    public const byte CommonMessageByteOption = 0;
    public const byte SystemMessageByteOption = 1;
    public const string NewUserAddedMessage = "USER_ADDED";
    public const string UserRemovedMessage = "USER_REMOVED";
    
    public string SenderUserName { get; set; }
    public DateTime SendingTime { get; set; }
    public string MessageText { get; set; }

    public MessageModel(string senderUsername, DateTime time, string messageText)
    {
        SenderUserName = senderUsername;
        SendingTime = time;
        MessageText = messageText;
    }

    public override string ToString()
    {
        return $"[{SendingTime}] {SenderUserName}: {MessageText}";
    }
}