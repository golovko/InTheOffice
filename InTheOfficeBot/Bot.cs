using System.Text;
using InTheOfficeBot;
using InTheOfficeBot.Helpers;
using InTheOfficeBot.Models;
using InTheOfficeBot.Repository;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class Bot
{
  private (string range, int number) _week => Helpers.GetWeekOrNextWeek();
  private string _welcomeMessage => $"Hiya!\nPlease choose your office days for the upcoming week: <b>{_week.range}</b>";
  private TelegramBotClient _bot;
  private CancellationTokenSource _cts = new();
  private IRepository _repo;
  private ILogger<Worker> _logger;
  private BotConfiguration _config;

  public Bot(ILogger<Worker> logger, BotConfiguration configuration, IRepository repo)
  {
    this._bot = new TelegramBotClient(configuration.BotToken, cancellationToken: _cts.Token);
    this._repo = repo ?? throw new ArgumentNullException(nameof(repo));
    this._logger = logger;
    this._config = configuration;
  }

  public async Task BotAction()
  {
    this._bot.OnError += OnError;
    this._bot.OnMessage += OnMessage;
    this._bot.OnUpdate += OnUpdate;
  }

  private async Task OnError(Exception exception, HandleErrorSource source)
  {
    this._logger.LogInformation(exception, exception.Message);
  }

  async Task OnMessage(Message msg, UpdateType type)
  {
    long chatId = msg.Chat.Id;

    var chat = this._repo.GetChat(msg.Chat.Id);
    if (chat == null)
    {
      chat = new()
      {
        ChatId = msg.Chat.Id,
        ChatName = msg.Chat.FirstName ?? msg.Chat.Title ?? msg.Chat.LastName,
      };

      this._repo.SaveChat(chat);
    }

    var user = _repo.GetUser(msg.From.Id);
    if (user == null)
    {
      user = _repo.SaveUser(new()
      {
        UserId = msg.From.Id,
        FirstName = msg.From.FirstName,
        Nickname = msg.From.Username,
      });
    }

    var message = msg.Text.Split("@")[0];

    if (msg.Chat.Type == ChatType.Private)
    {
      switch (message)
      {
        case "/set":
          var adminChats = _repo.GetChatsWhereUserIsAdmin(user);
          if (adminChats.Any())
          {

            var adminChatsNames = new InlineKeyboardMarkup();
            foreach (var adminChat in adminChats)
            {
              adminChatsNames.AddButton(adminChat.ChatName);
            }
            await this._bot.SendTextMessageAsync(chatId, "Select chat to manage", replyMarkup: new InlineKeyboardMarkup().AddButtons(string.Join(", ", adminChatsNames)), parseMode: ParseMode.Html);
          }

          await _bot.SendTextMessageAsync(chatId, "You don't have chats to manage", parseMode: ParseMode.Html);

          break;
      }
    }
    else
    {
      switch (message)
      {
        case "/start":
          string startMessage = "To use this bot, add it to a group chat and use the /poll command for the first time.\n" +
                                "The bot will send a reminder to choose days in the group chat every Friday morning to plan the next week.\n" +
                                "Use the /check command to see the latest answers.";
          await _bot.SendTextMessageAsync(chatId, startMessage, parseMode: ParseMode.Html);
          break;

        case "/check":
          string checkMessage = $"<b>Week: {_week.range}</b>\n{await ShowAnswers(msg.Chat.Id, _week.number)}";
          await _bot.SendTextMessageAsync(chatId, checkMessage, parseMode: ParseMode.Html);
          break;

        case "/poll":
          await _bot.SendTextMessageAsync(chatId, _welcomeMessage, parseMode: ParseMode.Html);
          var sentMsg = await SendReplyKeyboard(chatId);
          // PinMessage(sentMsg, chatId);
          // await _bot.UnpinChatMessageAsync(chatId, 123);
          //await _bot.PinChatMessageAsync(chatId, sentMsg.MessageId);
          break;

        case "/stop":
          //TODO: implement unsubscribing
          break;
      }

      case "/stat":
        await _bot.SendTextMessageAsync(chatId, GetStat(chatId), parseMode: ParseMode.Html);
        break;
    }
  }

  private string GetStat(long chatId)
  {
    var answers = _repo.GetAnswersByChatId(chatId);
    var totalDays = 0;
    var totalWeeks = 0;
    var groupedByUser = answers.GroupBy(a => a.UserId)
          .Select(g =>
          {
            var userAnswers = g.ToList();
            var lastAnswer = userAnswers.Last();
            int daysInTheOffice = userAnswers.Sum(a => a.SelectedDays.Count(b => b));
            int weeksInTheOffice = userAnswers.Count(a => a.SelectedDays.Any(b => b));

            totalDays += daysInTheOffice;
            totalWeeks += weeksInTheOffice;

            return new
            {
              UserId = g.Key,
              Username = lastAnswer.FirstName,
              DaysInTheOffice = daysInTheOffice,
              WeeksInTheOffice = weeksInTheOffice
            };
          })
          .ToList();

    var weeksCount = answers.Select(a => a.WeekOfTheYear).Distinct().Count();
    var usersCount = groupedByUser.Count;

    var statByUser = string.Join("\n", groupedByUser.Select(stat =>
@$"
User: {stat.Username}
Days in the office: {stat.DaysInTheOffice}
Weeks in the office: {stat.WeeksInTheOffice}
Average d/w: {Math.Round((double)stat.DaysInTheOffice / stat.WeeksInTheOffice, 1)}"));

    return @$"Here are the bot usage statistics:
- Weeks of usage: {weeksCount}
- Number of users: {usersCount}
- Total days in the office: {totalDays}
- Total weeks in the office: {totalWeeks}
- Average days per week: {Math.Round((double)totalDays/totalWeeks, 1)}
{statByUser}";
  }

  async Task OnUpdate(Update update)
  {

    if (update.CallbackQuery is not { } query)
    {
      return;
    }

    var user = this._repo.GetUser(query.From.Id);
    if (user == null)
    {
      user = this._repo.SaveUser(new()
      {
        UserId = query.From.Id,
        FirstName = query.From.FirstName,
        Nickname = query.From.Username,
      });
    }

    var chat = this._repo.GetChat(query.Message!.Chat.Id);
    if (chat == null)
    {
      chat = new()
      {
        ChatId = query.Message.Chat.Id,
        ChatName = query.Message.Chat.FirstName ?? query.Message.Chat.Title ?? query.Message.Chat.LastName,
      };
      this._repo.SaveChat(chat);
    }

    var admins = await _bot.GetChatAdministratorsAsync(query.Message.Chat.Id);
    foreach (var admin in admins)
    {
      var chatAdmin = new InTheOfficeBot.Models.User { UserId = admin.User.Id, FirstName = admin.User.FirstName, Nickname = admin.User.Username };
      if(chat.AdminIds is null || !chat.AdminIds.Contains(chatAdmin.Id))
      {
        chat.AddAdmin(chatAdmin);
      }
    }

    this._repo.UpdateChat(chat);

    await this._bot.AnswerCallbackQueryAsync(query.Id, $"You picked {query.Data}");
    var day = Enum.Parse<DayOfWeek>(query.Data!, true);
    var chatId = query.Message!.Chat.Id;
    var messageId = query.Message.MessageId;
    var firstName = query.From.FirstName;
    var userId = query.From.Id;
    var latestAnswer = _repo.GetLatestUserAnswer(chatId, _week.number, userId) ?? new() { Chat = chat, WeekOfTheYear = _week.number, User = user };
    latestAnswer.User.FirstName = firstName;
    latestAnswer.MessageId = messageId;

    var dayIndex = (int)day - 1;
    latestAnswer.SelectedDays[dayIndex] = !latestAnswer.SelectedDays[dayIndex];

    this._repo.SaveAnswer(latestAnswer);

    var updatedText = await ShowAnswers(chatId, _week.number);
    var replyMarkup = MessageHelpers.DaysKeyboard();

    await this._bot.EditMessageTextAsync(chatId, messageId, updatedText, replyMarkup: replyMarkup, parseMode: ParseMode.Html);
  }

  public async Task SendPoll()
  {
    var chatIds = this._repo.GetChatIds();
    foreach (var chatId in chatIds)
    {
      try
      {
        this._logger.LogInformation("Sending a message into the chat: " + chatId);
        await this._bot.SendTextMessageAsync(chatId, _welcomeMessage, parseMode: ParseMode.Html);
        await SendReplyKeyboard(chatId);
      }
      catch (System.Exception e)
      {
        this._logger.LogInformation("Can't send an update message to a chat, get an error: " + e.InnerException);
      }
    }
  }

  async Task<Message> SendReplyKeyboard(long chatId)
  {
    return await this._bot.SendTextMessageAsync(chatId, await ShowAnswers(chatId, _week.number), replyMarkup: MessageHelpers.DaysKeyboard(), parseMode: ParseMode.Html);
  }

  public bool[] SelectedDays(long chatId)
  {
    bool[] coveredDays = new bool[5];
    var answers = _repo.GetAnswersByWeek(chatId, this._week.number);
    foreach (var answer in answers)
    {
      for (var i = 0; i < answer.SelectedDays.Length; i++)
      {
        if (answer.SelectedDays[i]) coveredDays[i] = true;
      }
    }
    return coveredDays;
  }

  async Task<string> ShowAnswers(long chatId, int week)
  {
    var result = new StringBuilder();
    var s = SelectedDays(chatId);
    result.AppendFormat(@"<b>Days covered</b>:
Mo {0}  Tu {1}  We {2}  Th {3}  Fr {4}
",
       MessageHelpers.FormatDay(s[0]),
       MessageHelpers.FormatDay(s[1]),
       MessageHelpers.FormatDay(s[2]),
       MessageHelpers.FormatDay(s[3]),
       MessageHelpers.FormatDay(s[4]));
    result.Append("\n");

    var answersByWeek = _repo.GetAnswersByWeek(chatId, week);

    if (answersByWeek.Any())
    {
      foreach (var answer in answersByWeek)
      {
        // if (string.IsNullOrEmpty(answer.User?.FirstName) || answer.User.FirstName == "FirstName")
        // {
        //   try
        //   {
        //     var chatMember = await _bot.GetChatMemberAsync(chatId, answer.User.UserId);
        //     answer.User.FirstName = chatMember.User.FirstName;
        //     this._repo.SaveAnswer(answer);
        //   }
        //   catch
        //   {
        //     answer.User.FirstName = "NoName";
        //   }
        // }
        result.AppendFormat("{0,-10}{1}\n",
              $"<a href='tg://user?id={answer.User.UserId}'><code>{answer.User.FirstName.PadRight(9).Substring(0, 9)}</code></a>",
              MessageHelpers.FormatSelectedDays(answer.SelectedDays));
      }
    }
    return result.ToString();
  }

  async void PinMessage(Message msg, long chatId)
  {
    await _bot.PinChatMessageAsync(chatId, msg.MessageId);

  }
}