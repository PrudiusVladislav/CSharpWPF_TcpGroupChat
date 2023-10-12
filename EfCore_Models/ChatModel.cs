namespace Ef_Models;

public abstract class ChatModel
{
    public ICollection<Message> Messages { get; set; }
}