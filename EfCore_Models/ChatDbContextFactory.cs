using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ef_Models;

public class ChatDbContextFactory: IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[]? args = null)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.json")
            .Build();

        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlServer(config.GetConnectionString("SqlClient"))
            .Options;
        return new ChatDbContext(options);
    }
}