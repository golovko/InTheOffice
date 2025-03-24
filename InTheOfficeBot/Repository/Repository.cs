using System.Threading.Tasks;
using InTheOfficeBot.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace InTheOfficeBot.Repository;

public class Repository : IRepository
{
  private SqLiteContext _db;

  public Repository(SqLiteContext db)
  {
    _db = db ?? throw new ArgumentNullException(nameof(db));
  }

  public IEnumerable<Answer> GetAnswersByUser(long chatId, long userId)
  {
    return _db.Answers.Where(a => a.Chat.ChatId == chatId && a.User.UserId == userId);
  }

  public IEnumerable<Answer> GetAnswersByWeek(long chatId, int weekOfTheYear)
  {
    return _db.Answers.Where(a =>
      a.Chat.ChatId == chatId && a.WeekOfTheYear == weekOfTheYear && a.UpdatedAt.Year == DateTime.Now.Year);
  }

  public Chat GetChat(long chatId)
  {
    return _db.Chats.FirstOrDefault(c => c.ChatId == chatId);
  }

  public Chat GetChat(string chatName)
  {
    return _db.Chats.FirstOrDefault(c => c.ChatName == chatName);
  }

  public IEnumerable<long> GetChatIds(Func<Chat, bool> filter = null)
  {
    var query = _db.Chats.Where(c => c.ChatId < 0);
    if (filter != null)
    {
      query = (IQueryable<Chat>)query.Where(filter);
    }

    return query.Select(c => c.ChatId).Distinct();
  }

  public Answer? GetLatestUserAnswer(long chatId, int weekOfTheYear, long userId)
  {
    return _db.Answers.FirstOrDefault(a =>
      a.Chat.ChatId == chatId && a.User.UserId == userId && a.WeekOfTheYear == weekOfTheYear &&
      a.UpdatedAt.Year == DateTime.Now.Year);
  }

  public void SaveAnswer(Answer answer)
  {
    //var result = _db.Answers.FirstOrDefault(a => a.Chat.ChatId == answer.Chat.ChatId && a.User.UserId == answer.User.UserId && a.WeekOfTheYear == answer.WeekOfTheYear);
    var result = _db.Answers.FirstOrDefault(a => a.Id == answer.Id);
    if (result == null)
    {
      _db.Answers.Add(answer);
    }
    else
    {
      _db.Answers.Update(answer);
    }

    _db.SaveChanges();
  }

  public Chat SaveChat(Chat chat)
  {
    var isExist = _db.Chats.FirstOrDefault(c => c.ChatId == chat.ChatId);
    EntityEntry<Chat>? savedChat;
    if (isExist is null)
    {
      savedChat = _db.Chats.Add(chat);
    }
    else
    {
      savedChat = _db.Chats.Update(chat);
    }

    _db.SaveChanges();
    return savedChat.Entity;
  }

  public Chat UpdateChat(Chat chat)
  {
    var updatedChat = _db.Chats.Update(chat);
    _db.SaveChanges();
    return updatedChat.Entity;
  }

  public User GetUser(long userId)
  {
    return _db.Users.FirstOrDefault(u => u.UserId == userId);
  }

  public User SaveUser(User user)
  {
    var savedUser = _db.Users.Add(user);
    _db.SaveChanges();
    return savedUser.Entity;
  }

  public User UpdateUser(User user)
  {
    var updatedUser = _db.Users.Update(user);
    _db.SaveChanges();
    return updatedUser.Entity;
  }

  public IEnumerable<Chat> GetChatsWhereUserIsAdmin(User user)
  {
    return _db.Chats.Where(c => c.AdminIds.Contains(user.UserId));
  }

  public IEnumerable<Answer> GetAnswersByChatId(long chatId)
  {
    var answer = _db.Answers.Where(a => a.Chat.ChatId == chatId);
    return _db.Answers.Where(a => a.Chat.ChatId == chatId);
  }
}