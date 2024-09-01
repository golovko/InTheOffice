using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InTheOfficeBot.Models;
class Answer
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int Id { get; set; }
  public long ChatId { get; set; }
  public int WeekOfTheYear { get; set; }
  public long UserId { get; set; }
  public bool[] SelectedDays { get; set; } = new bool[5];
  public DateTime UpdatedAt { get; private set; } = DateTime.Now;

  public Answer() { }
  public Answer(long chatId, int weekOfTheYear, long userId)
  {
    this.ChatId = chatId;
    this.WeekOfTheYear = weekOfTheYear;
    this.UserId = userId;
  }
}