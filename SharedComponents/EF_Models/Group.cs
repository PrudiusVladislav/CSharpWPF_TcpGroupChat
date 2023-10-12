namespace SharedComponents.EF_Models;

public class Group : ChatModel
{
    public int Id { get; set; }
    public required string GroupName { get; set; } 

    public virtual ICollection<ClientsGroups> GroupMembers { get; set; } = new List<ClientsGroups>();
}