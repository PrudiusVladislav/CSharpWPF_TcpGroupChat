using System.ComponentModel.DataAnnotations;

namespace SharedComponents.EF_Models;

public class Client
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;

    public virtual ICollection<ClientsGroups> ClientGroups { get; set; } = new List<ClientsGroups>();
}