using InTheOfficeBot.Models;
namespace InTheOfficeBot.Repository;
public interface IRepository
{
  void SaveAnswer(Answer answer);
  IEnumerable<Answer> GetAnswersByUser(long chatId, long userId);
  IEnumerable<Answer> GetAnswersByWeek(long chatId, int weekOfTheYear);
  Answer? GetLatestUserAnswer(long chatId, int weekOfTheYear, long userId);
  long[] GetChatIds();
  Chat SaveChat(Chat chat);
  Chat UpdateChat(Chat chat);
  Chat GetChat(long chatId);
  User GetUser(long userId);
  User SaveUser(User user);
  User UpdateUser(User user);
  IEnumerable<Chat> GetChatsWhereUserIsAdmin(User user);
}
