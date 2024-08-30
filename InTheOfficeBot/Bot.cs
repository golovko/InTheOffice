using System.Globalization;
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

class Bot
{
  private string _welcomeMessage = $"Welcome!\nPlease choose your office days for the upcoming week: <b>{Helpers.GetWeekOrNextWeek().Item1}</b>";
  private TelegramBotClient _bot;
  private CancellationTokenSource _cts = new CancellationTokenSource();
  private IRepository _repo;
  private (string range, int number) _week = Helpers.GetWeekOrNextWeek();
  private ILogger<Worker> _logger;
  private BotConfiguration _config;

  public Bot(ILogger<Worker> logger, BotConfiguration configuration)
  {
    this._bot = new TelegramBotClient(configuration.BotToken, cancellationToken: _cts.Token);
    this._repo = new AnswersRepository();
    this._logger = logger;
    this._config = configuration;
  }

  public async Task BotAction()
  {
    var me = await _bot.GetMeAsync();
    _bot.OnError += OnError;
    _bot.OnMessage += OnMessage;
    _bot.OnUpdate += OnUpdate;
    
    Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
    Console.ReadLine();
    _cts.Cancel(); // stop the bot
  }

  private async Task OnError(Exception exception, HandleErrorSource source)
  {
    _logger.LogInformation(exception, exception.Message);
  }

  async Task OnMessage(Message msg, UpdateType type)
  {
    if (msg.Text == "/select")
    {
      await _bot.SendTextMessageAsync(msg.Chat.Id, _welcomeMessage, parseMode: ParseMode.Html);
      await SendReplyKeyboard(msg.Chat.Id);
    }
  }

  async Task OnUpdate(Update update)
  {
    if (update is { CallbackQuery: { } query }) // non-null CallbackQuery
    {
      await _bot.AnswerCallbackQueryAsync(query.Id, $"You picked {query.Data}");

      var day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), query.Data!, true);
      var chatId = query.Message!.Chat.Id;
      var messageId = query.Message.MessageId;

      Answer? answer = new Answer(chatId, _week.number, query.From.Id);
      var latestAnswer = _repo.GetLatestUserAnswer(answer);
      if (latestAnswer != null)
      {
        latestAnswer.SelectedDays[(int)day - 1] = !latestAnswer.SelectedDays[(int)day - 1];
        _repo.SaveAnswer(latestAnswer);
      }
      else
      {
        answer.SetDay((int)day - 1, !answer.SelectedDays[(int)day - 1]);
        _repo.SaveAnswer(answer);
      }
      await _bot.EditMessageTextAsync(chatId, messageId: messageId, text: ShowAnswers(chatId, _week.number), replyMarkup: InlineKeyboard(), parseMode: ParseMode.Html);
    }
  }

  public async Task SendMessageForNextWeek()
  {
    if (Helpers.SendNow(this._config.SendDateTime))
    {
      var chatIds = _repo.GetChatIds();
      foreach (var chatId in chatIds)
      {
        System.Console.WriteLine("Sending a message into the chat: " + chatId);
        await _bot.SendTextMessageAsync(chatId, _welcomeMessage, parseMode: ParseMode.Html);
        await SendReplyKeyboard(chatId);
      }
    }
  }


  async Task<Message> SendReplyKeyboard(long chatId)
  {
    return await _bot.SendTextMessageAsync(chatId, ShowAnswers(chatId, _week.number), replyMarkup: InlineKeyboard(), parseMode: ParseMode.Html);
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
    result.AppendFormat(@"<b>Somebody in the office on</b>:
--------------------------------------------------------
Mo {0}  Tu {1}  We {2}  Th {3}  Fr {4}
",
       FormatDay(s[0]),
       FormatDay(s[1]),
       FormatDay(s[2]),
       FormatDay(s[3]),
       FormatDay(s[4]));
    result.Append("--------------------------------------------------------\n");

    var answersByWeek = _repo.GetAnswersByWeek(chatId, week);

    foreach (var answer in answersByWeek)
    {
      var user = _bot.GetChatMemberAsync(chatId, answer.UserId).Result;
      result.AppendFormat("{0,-10}{1}\n",
            $"@{user.User.Username}",
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