using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InTheOfficeBot.Models;

public record Answer
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  public Chat Chat { get; set; }
  public long MessageId { get; set; }
  public int WeekOfTheYear { get; set; }
  public User User { get; set; }
  public bool[] SelectedDays { get; set; } = new bool[5];
  public DateTime UpdatedAt { get; private set; } = DateTime.Now;
}