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
    var message = msg.Text.Split("@")[0];
    switch (message)
    {
      case "/start":
        string startMessage = "To use this bot, add it to a group chat and use the /poll command for the first time.\n" +
                              "The bot will send a reminder to choose days in the group chat every Friday morning to plan the next week.\n" +
                              "Use the /check command to see the latest answers.";
        await _bot.SendTextMessageAsync(chatId, startMessage, parseMode: ParseMode.Html);
        break;

      case "/check":
        string checkMessage = $"<b>Week: {_week.range}</b>\n{ShowAnswers(msg.Chat.Id, _week.number)}";
        await _bot.SendTextMessageAsync(chatId, checkMessage, parseMode: ParseMode.Html);
        break;

      case "/poll":
        await _bot.SendTextMessageAsync(chatId, _welcomeMessage, parseMode: ParseMode.Html);
        await SendReplyKeyboard(chatId);
        break;
    }
  }

  async Task OnUpdate(Update update)
  {
    if (update.CallbackQuery is not { } query)
    {
      return;
    }

    await this._bot.AnswerCallbackQueryAsync(query.Id, $"You picked {query.Data}");

    var day = Enum.Parse<DayOfWeek>(query.Data!, true);
    var chatId = query.Message!.Chat.Id;
    var messageId = query.Message.MessageId;

    var userId = query.From.Id;
    var latestAnswer = _repo.GetLatestUserAnswer(chatId, _week.number, userId) ?? new Answer(chatId, _week.number, userId);

    var dayIndex = (int)day - 1;
    latestAnswer.SelectedDays[dayIndex] = !latestAnswer.SelectedDays[dayIndex];

    this._repo.SaveAnswer(latestAnswer);

    var updatedText = ShowAnswers(chatId, _week.number);
    var replyMarkup = InlineKeyboard();

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
    return await this._bot.SendTextMessageAsync(chatId, ShowAnswers(chatId, _week.number), replyMarkup: InlineKeyboard(), parseMode: ParseMode.Html);
  }

  InlineKeyboardMarkup InlineKeyboard()
  {
    return new InlineKeyboardMarkup()
      .AddButtons(DayOfWeek.Monday.ToString(), DayOfWeek.Tuesday.ToString(), DayOfWeek.Wednesday.ToString())
      .AddNewRow()
      .AddButton(DayOfWeek.Thursday.ToString())
      .AddButton(DayOfWeek.Friday.ToString());
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

  string ShowAnswers(long chatId, int week)
  {
    var result = new StringBuilder();
    var s = SelectedDays(chatId);
    var somebodyInTheOffice = false;
    result.AppendFormat(@"<b>Days covered</b>:
Mo {0}  Tu {1}  We {2}  Th {3}  Fr {4}
",
       FormatDay(s[0]),
       FormatDay(s[1]),
       FormatDay(s[2]),
       FormatDay(s[3]),
       FormatDay(s[4]));
    result.Append("\n");

    var answersByWeek = _repo.GetAnswersByWeek(chatId, week);
    if (somebodyInTheOffice)
    {
      result.Append("<b>Who is in the office:</b>\n");
    }
    foreach (var answer in answersByWeek)
    {
      var user = _bot.GetChatMemberAsync(chatId, answer.UserId).Result;
      result.AppendFormat("{0,-10}{1}\n",
            $"<a href='tg://user?id={user.User.Id}'>{user.User.FirstName}</a>",
            FormatSelectedDays(answer.SelectedDays));
    }
    return result.ToString();
  }

  private string FormatSelectedDays(bool[] selectedDays)
  {
    var formattedDays = new StringBuilder();

    foreach (var isSelected in selectedDays)
    {
      formattedDays.AppendFormat("{0,3}", isSelected ? "🌕" : "🌑"); //⚫🔴🟢
    }

    return formattedDays.ToString();
  }

  private string FormatDay(bool isSelected)
  {
    return isSelected ? "✅" : "❌";
  }
}