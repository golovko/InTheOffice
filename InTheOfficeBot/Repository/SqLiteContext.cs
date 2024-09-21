using Microsoft.EntityFrameworkCore;
using InTheOfficeBot.Models;

namespace InTheOfficeBot.Repository;

public class SqLiteContext : DbContext
{
  public DbSet<Answer> Answers { get; set; }

  private readonly string _connectionString;

  public SqLiteContext(IConfiguration configuration)
  {
    _connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? configuration.GetConnectionString("DefaultConnection")
        ?? throw new ArgumentNullException("Connection string not found.");
  }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    if (!optionsBuilder.IsConfigured)
    {
      
      if (string.IsNullOrWhiteSpace(_connectionString))
      {
        throw new InvalidOperationException("Connection string is not set.");
      }

      optionsBuilder.UseSqlite($"Data Source={_connectionString}");
    }
  }
}
