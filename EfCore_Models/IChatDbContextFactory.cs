using Microsoft.EntityFrameworkCore.Design;

namespace Ef_Models;

public interface IChatDbContextFactory<out TContext> where TContext : IChatDbContext
{
    TContext CreateDbContext(string[]? args = null);
}