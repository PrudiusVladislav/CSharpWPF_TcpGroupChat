using System.ComponentModel.DataAnnotations.Schema;

namespace SharedComponents.EF_Models;

public class PersonalChat : ChatModel
{
    public int Id { get; set; }
    public int FirstClientId { get; set; } 
    public int SecondClientId { get; set; } 
    
    //[ForeignKey("FirstClientId")]
    public virtual Client FirstClient { get; set; }
    //[ForeignKey("SecondClientId")]
    public virtual Client SecondClient { get; set; }
}