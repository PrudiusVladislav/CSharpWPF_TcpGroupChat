using System.ComponentModel.DataAnnotations;

namespace Ef_Models;

public abstract class ChatModel
{
    public int Id { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}