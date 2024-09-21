using Microsoft.EntityFrameworkCore;
using InTheOfficeBot.Models;
namespace InTheOfficeBot.Repository;
partial class SqLiteContext : DbContext
{
  public DbSet<Answer> Answers { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    optionsBuilder.UseSqlite($"Data Source={Path.Combine(homeDir, "data", "botdatabase.db")}");
  }
}