using System.ComponentModel.DataAnnotations.Schema;

namespace SharedComponents.EF_Models;

public class Message
{
    public int Id { get; set; }

    public DateTime TimeOfSending { get; set; }
    public int SenderClientId { get; set; }
    public int GroupId { get; set; }
    public string MessageContent { get; set; } = null!;
    
    public virtual Client SenderClient { get; set; }
    public virtual Group Group { get; set; }
}