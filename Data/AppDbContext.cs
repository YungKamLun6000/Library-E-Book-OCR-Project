using Microsoft.EntityFrameworkCore;

namespace LibraryEbookOcr.Data;

/// <summary>
/// EF Core session for the SQLite database (same idea as a SQLAlchemy Session).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Job> Jobs => Set<Job>();
}
