using Microsoft.EntityFrameworkCore;
using InTheOfficeBot.Models;
namespace InTheOfficeBot.Repository;
partial class SqLiteContext : DbContext
{
  public DbSet<Answer> Answers { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.UseSqlite("Data Source=/data/botdatabase.db");
  }
}