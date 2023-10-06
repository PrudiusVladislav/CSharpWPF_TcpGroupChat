

using System.Collections;

namespace SharedComponents.EF_Models;

public abstract class ChatModel
{
    public ICollection<Message> Messages { get; set; }
}