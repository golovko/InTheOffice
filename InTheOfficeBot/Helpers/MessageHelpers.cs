using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace InTheOfficeBot.Helpers;

public static class MessageHelpers
{
  public static InlineKeyboardMarkup DaysKeyboard()
  {
    return new InlineKeyboardMarkup()
      .AddButtons(DayOfWeek.Monday.ToString(), DayOfWeek.Tuesday.ToString(), DayOfWeek.Wednesday.ToString())
      .AddNewRow()
      .AddButton(DayOfWeek.Thursday.ToString())
      .AddButton(DayOfWeek.Friday.ToString());
  }

  public static string FormatSelectedDays(bool[] selectedDays)
  {
    var formattedDays = new StringBuilder();

    foreach (var isSelected in selectedDays)
    {
      formattedDays.AppendFormat("{0,3}", isSelected ? "🌕" : "🌑"); //⚫🔴🟢
    }

    return formattedDays.ToString();
  }

  public static string FormatDay(bool isSelected)
  {
    return isSelected ? "✅" : "❌";
  }
}