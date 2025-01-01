using System.Text;
using InTheOfficeBot;
using InTheOfficeBot.Helpers;
using InTheOfficeBot.Models;
using InTheOfficeBot.Repository;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
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
  private BotHelpers _helpers;

  public Bot(ILogger<Worker> logger, BotConfiguration configuration, IRepository repo)
  {
    this._bot = new TelegramBotClient(configuration.BotToken, cancellationToken: _cts.Token);
    this._repo = repo ?? throw new ArgumentNullException(nameof(repo));
    this._logger = logger;
    this._config = configuration;
    this._helpers = new BotHelpers(logger, configuration, repo);
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

    var user = _repo.GetUser(msg.From.Id);
    if (user == null)
    {
      user = _repo.SaveUser(new()
      {
        UserId = msg.From.Id,
        FirstName = msg.From.FirstName,
        Nickname = msg.From.Username ?? msg.From.FirstName,
      });
    }
    else
    {
      user.FirstName = msg.From.FirstName;
      user.Nickname = msg.From.Username ?? msg.From.FirstName ?? "NoName";
      this._repo.UpdateUser(user);
    }

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
    else
    {
      chat.ChatName = msg.Chat.FirstName ?? msg.Chat.Title ?? msg.Chat.LastName;
      this._repo.UpdateChat(chat);
    }

    var message = string.Empty;
    if (msg.Text is not null)
    {
      message = msg.Text.Split("@")[0];
    }

    if (msg.Chat.Type == ChatType.Private)
    {
      if (msg.ReplyToMessage is not null && msg.ReplyToMessage.Text?.Contains("weekly automatic sending poll") == true)
      {
        var scheduleChatId = msg.ReplyToMessage?.Text?.Split("#")[1];
        var scheduleChat = _repo.GetChat(long.Parse(scheduleChatId));
        if (scheduleChat is not null)
        {
          scheduleChat.SendPollOnDayOfWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), msg.Text.Split(" ")[0], true);
          scheduleChat.SendPollAt = TimeOnly.Parse(msg.Text.Split(" ")[1]);
          _repo.SaveChat(scheduleChat);
          await _bot.SendMessage(chatId, $"Scheduler for chat {scheduleChat.ChatName} successfully set to {scheduleChat.SendPollAt.ToShortTimeString()} each {scheduleChat.SendPollOnDayOfWeek}", parseMode: ParseMode.Html);
        }
      }

      if (msg.ReplyToMessage is not null && msg.ReplyToMessage.Text?.Contains("list of dates off") == true)
      {
        var scheduleChatId = msg.ReplyToMessage?.Text?.Split("#")[1];
        var scheduleChat = _repo.GetChat(long.Parse(scheduleChatId));
        var parsedDates = new List<DateOnly>();
        var dates = msg.Text.Split("\n").ToArray();
        foreach (var date in dates)
        {
          parsedDates.Add(DateOnly.Parse(date));
        }

        if (scheduleChat is not null)
        {
          scheduleChat.DaysOff = parsedDates;
          _repo.SaveChat(scheduleChat);
          await _bot.SendMessage(chatId, $"Off days for chat {scheduleChat.ChatName} successfully set");
        }
      }

      switch (message)
      {
        case "/set":
          var chats = _repo.GetChatIds();
          foreach (var id in chats)
          {
            var currentChat = _repo.GetChat(id);
            var admins = _bot.GetChatAdministrators(id).Result;

            currentChat.AdminIds = admins.Select(a => a.User.Id).ToList();
            _repo.SaveChat(currentChat);
          }

          var adminChats = _repo.GetChatsWhereUserIsAdmin(user);

          if (adminChats.Any())
          {
            var adminChatNames = new InlineKeyboardMarkup();
            foreach (var adminChat in adminChats)
            {
              adminChatNames.AddButton(adminChat.ChatName ?? "ChatWithoutName");
            }
            await this._bot.SendMessage(chatId, "Select chat to manage", replyMarkup: adminChatNames, parseMode: ParseMode.Html);
          }
          else
          {
            await _bot.SendMessage(chatId, "You don't have chats to manage", parseMode: ParseMode.Html);
          }
          break;

      }

    }
    else
    {
      var admins = await this._bot.GetChatAdministrators(chatId);

      switch (message)
      {
        case "/start":
          string startMessage = "To use this bot, add it to a group chat and use the /poll command for the first time.\n" +
                                "The bot will send a reminder to choose days in the group chat every Friday morning to plan the next week.\n" +
                                "Use the /check command to see the latest answers.";
          await _bot.SendMessage(chatId, startMessage, parseMode: ParseMode.Html);
          break;

        case "/check":
          string checkMessage = $"<b>Week: {_week.range}</b>\n{await _helpers.ShowAnswers(msg.Chat.Id, _week.number)}";
          await _bot.SendMessage(chatId, checkMessage, parseMode: ParseMode.Html);
          break;

        case "/poll":
          await _bot.SendMessage(chatId, _welcomeMessage, parseMode: ParseMode.Html);
          var sentMsg = await SendReplyKeyboard(chatId);
          // PinMessage(sentMsg, chatId);
          // await _bot.UnpinChatMessageAsync(chatId, 123);
          // await _bot.PinChatMessageAsync(chatId, sentMsg.MessageId);
          if (_repo.GetChat(chatId).PinLatestPoll)
          {
            PinMessage(sentMsg, chatId);
          }
          break;
      }
    }
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
        Nickname = query.From.Username ?? query.From.FirstName,
      });
    }
    else
    {
      user.FirstName = query.From.FirstName;
      user.Nickname = query.From.Username ?? query.From.FirstName;
      this._repo.UpdateUser(user);
    }

    long currentChatID = 0;
    //get currentChatID
    if (query.Message.Text == "Select chat to manage")
    {
      currentChatID = _repo.GetChat(query.Data).ChatId;
    }
    else
    {
      currentChatID = query.Message.Chat.Id;
    }

    var chat = this._repo.GetChat(currentChatID);
    // if (chat == null)
    // {
    //   chat = this._repo.SaveChat(new()
    //   {
    //     ChatId = currentChatID,
    //     ChatName = query.Message.Chat.FirstName ?? query.Message.Chat.Title ?? query.Message.Chat.LastName,
    //   });
    // }
    // else
    // {
    //   chat.ChatName = query.Message.Chat.FirstName ?? query.Message.Chat.Title ?? query.Message.Chat.LastName;
    //   this._repo.UpdateChat(chat);
    // }

    if (query.Message?.Chat.Type == ChatType.Private)
    {
      var messageText = string.Empty;
      var chatKeyboard = GetChatKeyboard(chat.ChatId);
      var stringId = "";
      if (query.Message.Text.Split("#").Length > 1)
      {
        stringId = query.Message.Text.Split("#")[1];
      }
      var parsed = long.TryParse(stringId, out var chatId);
      var currentChat = _repo.GetChat(chatId);

      switch (query.Data)
      {
        case "switchBotState":
          currentChat.IsStopped = !currentChat.IsStopped;
          this._repo.SaveChat(currentChat);
          await this._bot.AnswerCallbackQuery(query.Id, $"You just " + (currentChat.IsStopped ? "🛑 stopped" : "🚀 started") + " the bot");
          await this._bot.EditMessageReplyMarkup(chatId: query.Message.Chat.Id, messageId: query.Message.Id, replyMarkup: GetChatKeyboard(currentChat.ChatId));
          break;

        case "Scheduler":

          messageText = "Please reply to this message with a day and time for weekly automatic sending poll to the chat in a format: <b>Day HH:MM</b>\n" +
                        "Example: <b>Friday 10:00</b>\n\n" +
                        "Current scheduled day: " + currentChat.SendPollOnDayOfWeek + "\n" +
                        "Current scheduled time: " + currentChat.SendPollAt.ToShortTimeString() + "\n" +
                        "Chat: " + currentChat.ChatName + "\n" +
                        "Chat ID: #" + currentChat.ChatId;
          await this._bot.AnswerCallbackQuery(query.Id, $"You are about setting up scheduler for chat '{currentChat.ChatName}'");
          await this._bot.SendMessage(query.Message.Chat.Id, messageText, parseMode: ParseMode.Html);
          break;

        case "Off days":
          messageText = "Please reply to this message with a list of dates off in the format: <b>DD.MM</b>, each date on a separate line.\n" +
                "Example:\n<b>01.01\n08.03</b>\n\n" +
                "Current off-days: " + (currentChat.DaysOff != null ? string.Join("\n", currentChat.DaysOff.Select(d => d.ToShortDateString())) : "Not set yet") + "\n" +
                "Chat: " + currentChat.ChatName + "\n" +
                "Chat ID: #" + currentChat.ChatId;
          await this._bot.AnswerCallbackQuery(query.Id, $"You are about setting up scheduler for chat '{currentChat.ChatName}'");
          await this._bot.SendMessage(query.Message.Chat.Id, messageText, parseMode: ParseMode.Html);
          break;

        case "Auto pinning":
          currentChat.PinLatestPoll = !currentChat.PinLatestPoll;
          this._repo.SaveChat(currentChat);
          await this._bot.AnswerCallbackQuery(query.Id, "You turned auto pinning " + (currentChat.PinLatestPoll ? "🟢 on" : "🔴 off"));
          await this._bot.EditMessageReplyMarkup(chatId: query.Message.Chat.Id, messageId: query.Message.Id, replyMarkup: GetChatKeyboard(currentChat.ChatId));

          break;

        case "Get stats":
          await this._bot.AnswerCallbackQuery(query.Id, $"You picked {query.Data}");

          await _bot.SendMessage(query.Message.Chat.Id, _helpers.GetStat(currentChat.ChatId), parseMode: ParseMode.Html);

          break;

        default:
          await this._bot.AnswerCallbackQuery(query.Id, $"You picked {query.Data}");

          if (_repo.GetChatsWhereUserIsAdmin(user).Select(c => c.ChatName).Contains(query.Data))
          {
            messageText = $"Please select option to configure chat '{query.Data}' chat ID: #{_repo.GetChatsWhereUserIsAdmin(user).First(c => c.ChatName == query.Data).ChatId}";
          }
          await this._bot.EditMessageText(chatId: query.Message.Chat.Id, messageId: query.Message.Id, messageText, replyMarkup: chatKeyboard, parseMode: ParseMode.Html);

          break;

      }


    }
    else
    {
      bool isNonWorkingDay = _helpers.CheckNonWorkingDays(query.Data);
      // TODO: implement non-working days
      await this._bot.AnswerCallbackQuery(query.Id, $"You picked {query.Data}");

      var day = Enum.Parse<DayOfWeek>(query.Data!, true);
      var messageId = (int)query.Message?.MessageId;
      var latestAnswer = _repo.GetLatestUserAnswer(chat.ChatId, _week.number, user.UserId);

      if (latestAnswer is null)
      {
        latestAnswer = new Answer
        {
          Chat = chat,
          User = user,
          WeekOfTheYear = _week.number,
          MessageId = messageId,
          SelectedDays = new bool[5],
        };
      }

      var dayIndex = (int)day - 1;
      latestAnswer.SelectedDays[dayIndex] = !latestAnswer.SelectedDays[dayIndex];

      this._repo.SaveAnswer(latestAnswer);

      var updatedText = await _helpers.ShowAnswers(chat.ChatId, _week.number);
      await this._bot.EditMessageText(chat.ChatId, messageId, updatedText, replyMarkup: MessageHelpers.DaysKeyboard(), parseMode: ParseMode.Html);
    }
  }

  private InlineKeyboardMarkup GetChatKeyboard(long chatId)
  {
    var chat = _repo.GetChat(chatId);
    return new InlineKeyboardMarkup(new[] {
        new[] { InlineKeyboardButton.WithCallbackData(chat.IsStopped? "🚀 Start bot" : "🛑 Stop bot ", "switchBotState") },
        new[] { InlineKeyboardButton.WithCallbackData("🕘 Scheduler", "Scheduler") },
        new[] { InlineKeyboardButton.WithCallbackData("📆 Off days", "Off days") },
        new[] { InlineKeyboardButton.WithCallbackData(chat.PinLatestPoll ? "📌 Turn Auto Pinning Off": "📌 Turn Auto Pinning On", "Auto pinning") },
        new[] { InlineKeyboardButton.WithCallbackData("📈 Get stats", "Get stats") },
      });
  }

  public async Task SendPoll()
  {
    //TODO: move auto-messaging logic from worker here 
    var chatIds = this._repo.GetChatIds(c => !c.IsStopped);
    foreach (var chatId in chatIds)
    {
      try
      {
        this._logger.LogInformation("Sending a message into the chat: " + chatId);
        await this._bot.SendMessage(chatId, _welcomeMessage, parseMode: ParseMode.Html);
        var msg = await SendReplyKeyboard(chatId);
        if (_repo.GetChat(chatId).PinLatestPoll)
        {
          PinMessage(msg, chatId);
        }
      }
      catch (System.Exception e)
      {
        this._logger.LogInformation("Can't send an update message to a chat, get an error: " + e.InnerException);
      }
    }
  }

  async Task<Message> SendReplyKeyboard(long chatId)
  {
    return await this._bot.SendMessage(chatId, await _helpers.ShowAnswers(chatId, _week.number), replyMarkup: MessageHelpers.DaysKeyboard(), parseMode: ParseMode.Html);
  }

  async void PinMessage(Message msg, long chatId)
  {
    try
    {
      await _bot.UnpinChatMessage(chatId);
    }
    catch (System.Exception)
    {
      this._logger.LogInformation("Can't unpin a message to a chat");
    } 
        try
    {
      await _bot.PinChatMessage(chatId, msg.MessageId);
    }
    catch (System.Exception)
    {
      this._logger.LogInformation("Can't pin a message to a chat");
    } 
  }
}