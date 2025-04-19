using Microsoft.EntityFrameworkCore;

namespace demo.Sqlite;

public class FactoryContext : DbContext
{
    public string DbFilePath { get; set; } = "Factory.db";

    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<SalesOrder> SalesOrders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbFilePath}");
    }
}