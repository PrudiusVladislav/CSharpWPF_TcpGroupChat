namespace SharedComponents.EF_Models;

public class ClientsGroups
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int GroupId { get; set; }
    
    public virtual Client Client { get; set; }
    public virtual Group Group { get; set; }
}