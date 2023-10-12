using System.ComponentModel.DataAnnotations;

namespace SharedComponents.EF_Models;

public class Client
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }

    public virtual ICollection<ClientsGroups> ClientGroups { get; set; } = new List<ClientsGroups>();
}