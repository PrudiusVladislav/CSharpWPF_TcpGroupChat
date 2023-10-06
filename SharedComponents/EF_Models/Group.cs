namespace SharedComponents.EF_Models;

public class Group : ChatModel
{
    public int Id { get; set; }
    public string GroupName { get; set; } = null!;

    public virtual ICollection<ClientsGroups> GroupClients { get; set; } = new List<ClientsGroups>();
}