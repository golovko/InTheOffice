using Microsoft.EntityFrameworkCore;

namespace InTheOfficeBot.Models;
class Answer
{

  public int Id { get; set;}
  public long ChatId { get; set; }
  public int WeekOfTheYear { get; set; }
  public long UserId { get; set; }
  public bool[] SelectedDays { get; set; } = { false, false, false, false, false };

  public Answer(){}
  public Answer(long chatId, int weekOfTheYear, long userId, bool[] selectedDays){
    this.ChatId = chatId;
    this.WeekOfTheYear = weekOfTheYear;
    this.UserId = userId;
    this.SelectedDays = selectedDays;
  }
}