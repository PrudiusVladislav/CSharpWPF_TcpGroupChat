using Microsoft.EntityFrameworkCore;

namespace SharedComponents.EF_Models;

public class ChatDbContext: DbContext
{
    public DbSet<Client> Clients { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<ClientsGroups> ClientsGroups { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    
    public ChatDbContext()
    {
        
    }
    public ChatDbContext(DbContextOptions<ChatDbContext> options)
        : base(options)
    {

    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>()
            .HasIndex(c => c.Username)
            .IsUnique();
        
        modelBuilder.Entity<Group>()
            .HasIndex(g => g.GroupName)
            .IsUnique();
        
    }
}