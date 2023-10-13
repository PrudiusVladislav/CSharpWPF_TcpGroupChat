using Microsoft.EntityFrameworkCore;

namespace Ef_Models;

public class ChatDbContext: DbContext
{
    public DbSet<Client> Clients { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<PersonalChat> PersonalChats { get; set; } = null!;
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
        
        modelBuilder.Entity<PersonalChat>()
            .HasOne(c => c.FirstClient)
            .WithMany()
            .HasForeignKey(c => c.FirstClientId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        modelBuilder.Entity<PersonalChat>()
            .HasOne(c => c.SecondClient)
            .WithMany()
            .HasForeignKey(c => c.SecondClientId)
            .OnDelete(DeleteBehavior.ClientSetNull);
        
    }
}