using InTheOfficeBot.Models;
namespace InTheOfficeBot.Repository;
interface IRepository
{
  void SaveAnswer(Answer answer);
  IEnumerable<Answer> GetAnswersByUser(long chatId, long userId);
  IEnumerable<Answer> GetAnswersByWeek(long chatId, int weekOfTheYear);
}
