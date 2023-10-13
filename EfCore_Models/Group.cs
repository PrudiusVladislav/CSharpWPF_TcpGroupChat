namespace Ef_Models;

public class Group : ChatModel
{
    public required string GroupName { get; set; } 

    public virtual ICollection<ClientsGroups> GroupMembers { get; set; } = new List<ClientsGroups>();
}