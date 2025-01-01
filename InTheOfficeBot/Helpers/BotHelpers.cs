using System.Text;
using InTheOfficeBot;
using InTheOfficeBot.Helpers;
using InTheOfficeBot.Models;
using InTheOfficeBot.Repository;

public class BotHelpers
{
  private IRepository _repo;
  private ILogger<Worker> _logger;
  private BotConfiguration _config;


  public BotHelpers(ILogger<Worker> logger, BotConfiguration configuration, IRepository repo)
  {
    this._repo = repo ?? throw new ArgumentNullException(nameof(repo));
    this._logger = logger;
    this._config = configuration;
  }

  public string GetStat(long chatId)
  {
    var answers = _repo.GetAnswersByChatId(chatId);
    var totalDays = 0;
    var totalWeeks = 0;
    var groupedByUser = new List<dynamic>();
    var userGroups = new Dictionary<long, List<Answer>>();

    foreach (var answer in answers)
    {
      if (answer.User != null)
      {
        if (!userGroups.ContainsKey(answer.User.UserId))
        {
          userGroups[answer.User.UserId] = new List<Answer>();
        }
        userGroups[answer.User.UserId].Add(answer);
      }
    }

    foreach (var userGroup in userGroups)
    {
      var userAnswers = userGroup.Value;
      var lastAnswer = userAnswers.Last();
      int daysInTheOffice = 0;
      int weeksInTheOffice = 0;

      foreach (var userAnswer in userAnswers)
      {
        daysInTheOffice += userAnswer.SelectedDays.Count(b => b);
        if (userAnswer.SelectedDays.Any(b => b))
        {
          weeksInTheOffice++;
        }
      }

      groupedByUser.Add(new
      {
        UserId = lastAnswer.User?.UserId ?? 0,
        Username = lastAnswer.User?.FirstName ?? "Unknown",
        DaysInTheOffice = daysInTheOffice,
        WeeksInTheOffice = weeksInTheOffice
      });
    }

    totalDays = groupedByUser.Sum(stat => stat.DaysInTheOffice);
    totalWeeks = groupedByUser.Sum(stat => stat.WeeksInTheOffice);

    var weeksCount = answers.Select(a => a.WeekOfTheYear).Distinct().Count();
    var usersCount = groupedByUser.Count;

    var statByUser = string.Join("\n", groupedByUser.Select(stat =>
@$"
User: #{stat.UserId} {stat.Username}
Days in the office: {stat.DaysInTheOffice}
Weeks in the office: {stat.WeeksInTheOffice}
Average d/w: {Math.Round((double)stat.DaysInTheOffice / stat.WeeksInTheOffice, 1)}"));

    return @$"Here are the bot usage statistics for chat {_repo.GetChat(chatId)?.ChatName}:
- Weeks of usage: {weeksCount}
- Number of users: {usersCount}
- Total days in the office: {totalDays}
- Total weeks in the office: {totalWeeks}
- Average days per week: {Math.Round((double)totalDays / totalWeeks, 1)}
{statByUser}";
  }

  public async Task<string> ShowAnswers(long chatId, int week)
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
        if (answer.User == null)
        {
          answer.User = new User { FirstName = "NoName" };
        }

        result.AppendFormat("{0,-10}{1}\n",
              $"<code><a href='tg://user?id={answer.User.UserId}'>{answer.User.FirstName.PadRight(9).Substring(0, 9)}</a></code>",
              MessageHelpers.FormatSelectedDays(answer.SelectedDays));
      }
    }
    return result.ToString();
  }

  internal bool CheckNonWorkingDays(string? data){
    return false;
  }

  private bool[] SelectedDays(long chatId)
  {
    bool[] coveredDays = new bool[5];
    var answers = _repo.GetAnswersByWeek(chatId, Helpers.GetWeekOrNextWeek().Item2);
    foreach (var answer in answers)
    {
      for (var i = 0; i < answer.SelectedDays.Length; i++)
      {
        if (answer.SelectedDays[i]) coveredDays[i] = true;
      }
    }
    return coveredDays;
  }

  public void UpdateChatAdmins(){
    //     var admins = await _bot.GetChatAdministrators(query.Message.Chat.Id);
    // foreach (var admin in admins)
    // {
    //     var chatAdmin = new InTheOfficeBot.Models.User { UserId = admin.User.Id, FirstName = admin.User.FirstName, Nickname = admin.User.Username };
    //     if (chat.AdminIds is null || !chat.AdminIds.Contains(chatAdmin.Id))
    //     {
    //         chat.AddAdmin(chatAdmin);
    //     }
    // }

    // this._repo.UpdateChat(chat);
  }
}