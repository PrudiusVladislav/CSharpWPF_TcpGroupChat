using System.ComponentModel.DataAnnotations.Schema;

namespace Ef_Models;

public class Message
{
    public int Id { get; set; }
    public required DateTime TimeOfSending { get; set; }
    public int SenderClientId { get; set; }
    public int ChatModelId { get; set; }
    public required string MessageContent { get; set; }
    
    public virtual Client SenderClient { get; set; }
    public virtual ChatModel ChatModel { get; set; }
}