using Microsoft.EntityFrameworkCore;
using InTheOfficeBot.Models;
namespace InTheOfficeBot.Repository;
class Context : DbContext
{
  public DbSet<Answer> Answers { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.UseSqlite("Data Source=botdatabase.db");
  }
}