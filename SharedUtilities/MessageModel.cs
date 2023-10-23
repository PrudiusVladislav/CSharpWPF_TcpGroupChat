namespace SharedUtilities;

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
    public const string NewUserAddedMessage = "ADDED_USER";
    public const string UserRemovedMessage = "REMOVED_USER";
    public const string AddGroupRequestMessage = "GROUP_ADD";
    public const string GroupRemoved = "REMOVED_GROUP";
    public const string GroupAddedMessage = "ADDED_GROUP";
    public const char MessageSeparator = '|';
    
    public string SenderUserName { get; set; }
    public DateTime SendingTime { get; set; }
    public string MessageText { get; set; }

    public MessageModel(string senderUsername, DateTime time, string messageText)
    {
        SenderUserName = senderUsername;
        SendingTime = time;
        MessageText = messageText;
    }

    public static string GetMessagePart(string message, int partNumber)
    {
        if (partNumber > message.Count(c => c == MessageModel.MessageSeparator)) return string.Empty;
        
        var startIndex = GetIndexOfNthOccurrence(message, MessageModel.MessageSeparator, partNumber) + 1;
        var endIndex = GetIndexOfNthOccurrence(message, MessageModel.MessageSeparator, partNumber + 1);

        return endIndex == -1 ? message[startIndex..] : message[startIndex..endIndex];
    }

    public static int GetIndexOfNthOccurrence(string input, char charToSeek, int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n), "Value of n must be greater than 0");
        if (n == 0)
            return -1;

        var index = -1;
        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] != charToSeek) continue;
            n--;
            
            if (n != 0) continue;
            index = i;
            break;
        }
        return index;
    }

    public static string FormMessage(params string[] arguments)
    {
        return string.Join(MessageSeparator, arguments);
    }
    
    public override string ToString()
    {
        return $"[{SendingTime}] {SenderUserName}: {MessageText}";
    }
}