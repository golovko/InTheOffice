using System.Globalization;
using InTheOfficeBot.Models;
using Microsoft.VisualBasic;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Globalization;
using InTheOfficeBot.Repository;
using Microsoft.AspNetCore.Components;


namespace InTheOfficeBot;

public class Worker : BackgroundService
{
    private bool[] isSelected = { false, false, false, false, false };
    //List<string> buttons => [DayOfWeek.Monday.ToString() + (selected[0]?"+":""), "Tuesday", "Wednesday", "Thursday", "Friday"];
    private string welcomeMessage = $"Welcome!\nPick days in the office for the next week: <b>{GetNextWeek()}</b>";
    private readonly ILogger<Worker> _logger;
    private TelegramBotClient _bot;
    private CancellationTokenSource _cts = new CancellationTokenSource();
    private int pollId = 0;
    private Answer answer = new Answer();
    private AnswerRepository _repo;

    public Worker(ILogger<Worker> logger, string token)
    {
        _logger = logger;
        _bot = new TelegramBotClient(token, cancellationToken: _cts.Token);
        _repo = new AnswerRepository("/answers");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await BotAction();
    }

    async Task BotAction()
    {
        var me = await _bot.GetMeAsync();
        _bot.OnError += OnError;
        _bot.OnMessage += OnMessage;
        _bot.OnUpdate += OnUpdate;

        Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
        Console.ReadLine();
        _cts.Cancel(); // stop the bot
        _logger.LogWarning("Bot stopped");
    }

    // method to handle errors in polling or in your OnMessage/OnUpdate code
    async Task OnError(Exception exception, HandleErrorSource source)
    {
        _logger.LogError(exception.Message);
    }

    // method that handle messages received by the bot:
    async Task OnMessage(Message msg, UpdateType type)
    {
        var chatId = msg.Chat.Id;

        if (msg.Text == "/start")
        {
            await _bot.SendTextMessageAsync(chatId, welcomeMessage, parseMode: ParseMode.Html);
            //await _bot.SendTextMessageAsync(chatId,"", replyMarkup: new ReplyKeyboardMarkup().AddButton("Choose days") );
        }
        else if (msg.Text == "/poll")
        {
            if (pollId != 0)
            {
                await _bot.SendTextMessageAsync(chatId, "Please give your response on current poll");
                //await _bot.ForwardMessageAsync(msg.Chat.Id, msg.Chat.Id, pollId, null, true, true);
            }
            var pollOptions = new List<InputPollOption>();
            pollOptions.AddRange(["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]);
            var poll = await _bot.SendPollAsync(chatId, $"What days are you going to attend next week: {GetNextWeek()}",
                pollOptions,
                isAnonymous: false,
                allowsMultipleAnswers: true
                );
            pollId = poll.MessageId;
            System.Console.WriteLine(poll.MessageId);

        }
        else if (msg.Text == "/select")
        {
            await SendReplyKeyboard(msg);
        }
    }

    async Task OnUpdate(Update update)
    {
        if (update is { CallbackQuery: { } query }) // non-null CallbackQuery
        {
            if(query.Data != "Submit"){
            await _bot.AnswerCallbackQueryAsync(query.Id, $"You picked {query.Data}");
            //await _bot.SendTextMessageAsync(query.Message!.Chat, $"User {query.From} clicked on {query.Data} chatId:{update.Id}, msgId: {pollId}");

            var day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), query.Data.Split(" ")[1], true);
            isSelected[(int)day - 1] = !isSelected[(int)day - 1];
            
            await _bot.EditMessageReplyMarkupAsync(chatId: query.Message!.Chat, replyMarkup: UpdateKeyboard(isSelected), messageId: pollId);
            var (range, nextWeek) = GetNextWeek();
            var answer = new Answer(query.Message!.Chat.Id, nextWeek, query.From.Id, isSelected);
            }
            _repo.SaveAnswer(answer);
        }
        
    }

    async Task<Message> SendReplyKeyboard(Message msg)
    {
        var (_, week) = GetNextWeek();
        var response = await _bot.SendTextMessageAsync(msg.Chat, welcomeMessage, replyMarkup: UpdateKeyboard(isSelected), parseMode: ParseMode.Html);
        await _bot.SendTextMessageAsync(msg.Chat, ShowAnswers(msg.Chat.Id, week), parseMode: ParseMode.Html);
        pollId = response.MessageId;
        return response;
    }

    // InlineKeyboardMarkup UpdateKeyboard2(bool[] isSelected)
    // {
    //     var replyMarkup = new InlineKeyboardMarkup()
    //                 .AddButtons("Mon" + (isSelected[0]?" ✅": " ❌"), "Tue" + (isSelected[1] ? " ✅" : " ❌"), "Wed" + (isSelected[2] ? " ✅" : " ❌"))
    //                 .AddNewRow()
    //                 .AddButton("Thu" + (isSelected[3] ? " ✅" : " ❌"))
    //                 .AddButton("Fri" + (isSelected[4] ? " ✅" : " ❌"))
    //                 .AddNewRow()
    //                 .AddButton("Submit");
    //     return replyMarkup;
    // }

    // InlineKeyboardMarkup UpdateKeyboard3(bool[] isSelected)
    // {
    //     var replyMarkup = new InlineKeyboardMarkup()
    //                 .AddButtons(DayOfWeek.Monday.ToString() + (isSelected[0] ? " ✅" : " ❌"), DayOfWeek.Tuesday.ToString() + (isSelected[1] ? " ✅" : " ❌"), DayOfWeek.Wednesday.ToString() + (isSelected[2] ? " ✅" : " ❌"))
    //                 .AddNewRow()
    //                 .AddButton(DayOfWeek.Thursday.ToString() + (isSelected[3] ? " ✅" : " ❌"))
    //                 .AddButton(DayOfWeek.Friday.ToString() + (isSelected[4] ? " ✅" : " ❌"))
    //                 .AddNewRow()
    //                 .AddButton("Submit");
    //     return replyMarkup;
    // }

    InlineKeyboardMarkup UpdateKeyboard(bool[] isSelected)
    {
        var replyMarkup = new InlineKeyboardMarkup()
                    .AddButtons((isSelected[0] ? "✅ " : "❌ ") + DayOfWeek.Monday.ToString(), (isSelected[1] ? "✅ " : "❌ ") + DayOfWeek.Tuesday.ToString(), (isSelected[2] ? "✅ " : "❌ ") + DayOfWeek.Wednesday.ToString())
                    .AddNewRow()
                    .AddButton((isSelected[3] ? "✅ " : "❌ ") + DayOfWeek.Thursday.ToString())
                    .AddButton((isSelected[4] ? "✅ " : "❌ ") + DayOfWeek.Friday.ToString())
                    .AddNewRow()
                    .AddButton("Submit");
        return replyMarkup;
    }

    string ShowAnswers(long chatId, int week){
        //var answersByWeek = _repo.GetAnswersByWeek(chatId, week);
        var answers = @"Sergii: Mon, Tue, Thu";
        return answers;
    }

    public static (string, int) GetNextWeek()
    {
        DateTime today = DateTime.Today;
        int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        DateTime nextMonday = today.AddDays(daysUntilMonday);
        DateTime nextFriday = nextMonday.AddDays(4);

        string startDate = nextMonday.ToString("dd MMM", CultureInfo.InvariantCulture);
        string endDate = nextFriday.ToString("dd MMM", CultureInfo.InvariantCulture);

        return ($"{startDate} - {endDate}", ISOWeek.GetWeekOfYear(nextMonday) + 1);
    }

}