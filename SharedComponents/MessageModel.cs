namespace SharedComponents;

public class MessageModel
{
    public const string ExitMessage = "EXIT";
    public const string ExitMessageResponse = "EXIT_RESPONSE";
    public const string ClientDisconnectMessage = "You have been disconnected";
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