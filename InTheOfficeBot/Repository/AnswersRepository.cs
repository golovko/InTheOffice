using InTheOfficeBot.Models;

namespace InTheOfficeBot.Repository;
class AnswersRepository : IRepository
{
  private SqLiteContext _db;
  public AnswersRepository()
  {
    _db = new SqLiteContext();
  }

  public IEnumerable<Answer> GetAnswersByUser(long chatId, long userId)
  {
    return _db.Answers.Where(a => a.ChatId == chatId && a.UserId == userId);
  }

  public IEnumerable<Answer> GetAnswersByWeek(long chatId, int weekOfTheYear)
  {
    return _db.Answers.Where(a => a.ChatId == chatId && a.WeekOfTheYear == weekOfTheYear);
  }

  public long[] GetChatIds()
  {
    return _db.Answers.Select(c => c.ChatId).Distinct().ToArray();
  }

  public Answer? GetLatestUserAnswer(long chatId, int weekOfTheYear, long userId)
  {
    return _db.Answers.FirstOrDefault(a => a.ChatId == chatId && a.UserId == userId && a.WeekOfTheYear == weekOfTheYear);
  }

  public void SaveAnswer(Answer answer)
  {
    var result = _db.Answers.FirstOrDefault(a => a.ChatId == answer.ChatId && a.UserId == answer.UserId && a.WeekOfTheYear == answer.WeekOfTheYear);
    if (result == null)
    {
      _db.Answers.Add(answer);
    }
    else
    {
      result.SelectedDays = answer.SelectedDays;
    }
    _db.SaveChanges();
  }
}