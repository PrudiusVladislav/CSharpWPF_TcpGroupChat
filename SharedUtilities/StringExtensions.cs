namespace SharedUtilities;

public static class StringExtensions
{
    public static string GetMessagePart(this string message, int partNumber)
    {
        if (partNumber > message.Count(c => c == MessageModel.MessageSeparator)) return string.Empty;
        
        var startIndex = message.GetIndexOfNthOccurrence(MessageModel.MessageSeparator, partNumber) + 1;
        var endIndex = message.GetIndexOfNthOccurrence(MessageModel.MessageSeparator, partNumber + 1);

        return endIndex == -1 ? message[startIndex..] : message[startIndex..endIndex];
    }

    public static int GetIndexOfNthOccurrence(this string input, char charToSeek, int n)
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
}