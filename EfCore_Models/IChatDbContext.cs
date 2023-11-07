using Microsoft.EntityFrameworkCore;

namespace Ef_Models;

public interface IChatDbContext: IDisposable, IAsyncDisposable
{
    public DbSet<Client> Clients { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<PersonalChat> PersonalChats { get; set; }
    public DbSet<ClientsGroups> ClientsGroups { get; set; }
    public DbSet<Message> Messages { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken)); 
}